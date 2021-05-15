using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile {

        //for now there will be NO friendly fire!
        [JsonPropertyAttribute] private IWarfare origin;

        [JsonPropertyAttribute] private float remainingTravelDistance;
        [JsonPropertyAttribute] private SeriaziableVector2 _position;
        [JsonPropertyAttribute] private SeriaziableVector2 _destination;
        [JsonPropertyAttribute] private ITargetable target;
        [JsonPropertyAttribute] public bool HasHitbox;
        [JsonPropertyAttribute] public bool Impact;
        [JsonPropertyAttribute] public int ImpactRange;
        [JsonPropertyAttribute] public string SpriteName = "cannonball_1";
        private float Speed = 2f;
        public Vector2 Position { get { return _position; } protected set { _position = value; } }
        private Vector2 Destination { get { return _destination.Vec; } set { _destination.Vec = value; } }

        public SeriaziableVector2 Velocity { get; internal set; }

        private Action<Projectile> cbOnDestroy;
        private Action<Projectile> cbOnChange;

        public Projectile() {
        }

        public Projectile(IWarfare origin, Vector3 startPosition, ITargetable target, Vector2 destination,
            Vector3 move, float travelDistance, bool HasHitbox, float speed = 2, bool impact = false, int impactRange = 1) {
            Speed = speed;
            remainingTravelDistance = travelDistance;
            Velocity = move * speed;
            _position = startPosition;
            _destination = destination; // needs some kind of random factor
            this.origin = origin;
            this.target = target;
            this.HasHitbox = HasHitbox;
            this.Impact = impact;
            this.ImpactRange = impactRange;
        }

        public void Update(float deltaTime) {
            //Vector3 dir = Destination - Position;
            //if (dir.magnitude < 0.1f) {
            //    Destroy();
            //}
            if (remainingTravelDistance < 0) {
                Destroy();
                return;
            }
            Vector2 dir = Velocity.Vec * deltaTime;
            remainingTravelDistance -= dir.magnitude;
            Position += dir;
        }

        private void Destroy() {
            if (Impact) {
                List<Tile> tiles = Util.CalculateCircleTiles(ImpactRange, 0, 0, Position.x, Position.y);
                foreach (Tile t in tiles) {
                    if (t.Structure != null) {
                        t.Structure.ReduceHealth(origin.CurrentDamage); //TODO: think about this.
                    }
                }
                //TODO: show impact crater
            }
            cbOnDestroy?.Invoke(this);
        }

        public bool OnHit(ITargetable hit) {
            if (ConfirmHit(hit) == false)
                return false;
            if (hit.IsAttackableFrom(origin) == false)
                return false;
            hit.TakeDamageFrom(origin);
            Destroy();
            return true;
        }

        private bool ConfirmHit(ITargetable hit) {
            //Does it have to be the targeted unit it damages???
            //if (hit == target)
            //    return true;
            if (hit.PlayerNumber == origin.PlayerNumber)
                return false;
            if (PlayerController.Instance.ArePlayersAtWar(origin.PlayerNumber, hit.PlayerNumber)) {
                return true;
            }
            return false;
        }

        public void RegisterOnDestroyCallback(Action<Projectile> cb) {
            cbOnDestroy += cb;
        }

        public void UnregisterOnDestroyCallback(Action<Projectile> cb) {
            cbOnDestroy -= cb;
        }
        //////////////////////////////////////////////////////////////////////////////
        //This implies that no solution exists for this situation as the target may literally outrun the projectile with its current direction
        //In cases like that, we simply aim at the place where the target will be 1 to 5 seconds from now.
        //Feel free to randomize t at your discretion for your specific game situation if you want that guess to feel appropriately noisier
        private static float PredictiveAimWildGuessAtImpactTime() {
            return UnityEngine.Random.Range(1, 5);
        }

        //////////////////////////////////////////////////////////////////////////////
        //returns true if a valid solution is possible
        //projectileVelocity will be a non-normalized vector representing the muzzle velocity of a lobbed projectile in 3D space
        //if it returns false, projectileVelocity will be filled with a reasonable-looking attempt
        //The reason we return true/false here instead of Vector3 is because you might want your AI to hold that shot until a solution exists
        //This is meant to hit a target moving at constant velocity
        //Full derivation by Kain Shin exists here:
        //http://www.gamasutra.com/blogs/KainShin/20090515/83954/Predictive_Aim_Mathematics_for_AI_Targeting.php
        //gravity is assumed to be a positive number. It will be calculated in the downward direction, feel free to change that if you game takes place in Spaaaaaaaace
        static public bool PredictiveAim(Vector3 muzzlePosition, float projectileSpeed, Vector3 targetPosition, Vector3 targetVelocity, float gravity, out Vector3 projectileVelocity, out Vector3 projectileDestination) {
            Debug.Assert(projectileSpeed > 0, "What are you doing shooting at something with a projectile that doesn't move?");
            if (muzzlePosition == targetPosition) {
                //Why dost thou hate thyself so?
                //Do something smart here. I dunno... whatever.
                projectileVelocity = projectileSpeed * (UnityEngine.Random.rotation * Vector3.forward);
                projectileDestination = muzzlePosition;
                return true;
            }

            //Much of this is geared towards reducing floating point precision errors
            float projectileSpeedSq = projectileSpeed * projectileSpeed;
            float targetSpeedSq = targetVelocity.sqrMagnitude; //doing this instead of self-multiply for maximum accuracy
            float targetSpeed = Mathf.Sqrt(targetSpeedSq);
            Vector3 targetToMuzzle = muzzlePosition - targetPosition;
            float targetToMuzzleDistSq = targetToMuzzle.sqrMagnitude; //doing this instead of self-multiply for maximum accuracy
            float targetToMuzzleDist = Mathf.Sqrt(targetToMuzzleDistSq);
            Vector3 targetToMuzzleDir = targetToMuzzle;
            targetToMuzzleDir.Normalize();

            Vector3 targetVelocityDir = targetVelocity;
            targetVelocityDir.Normalize();

            //Law of Cosines: A*A + B*B - 2*A*B*cos(theta) = C*C
            //A is distance from muzzle to target (known value: targetToMuzzleDist)
            //B is distance traveled by target until impact (targetSpeed * t)
            //C is distance traveled by projectile until impact (projectileSpeed * t)
            float cosTheta = Vector3.Dot(targetToMuzzleDir, targetVelocityDir);

            bool validSolutionFound = true;
            float t;
            if (Mathf.Approximately(projectileSpeedSq, targetSpeedSq)) {
                //a = projectileSpeedSq - targetSpeedSq = 0
                //We want to avoid div/0 that can result from target and projectile traveling at the same speed
                //We know that C and B are the same length because the target and projectile will travel the same distance to impact
                //Law of Cosines: A*A + B*B - 2*A*B*cos(theta) = C*C
                //Law of Cosines: A*A + B*B - 2*A*B*cos(theta) = B*B
                //Law of Cosines: A*A - 2*A*B*cos(theta) = 0
                //Law of Cosines: A*A = 2*A*B*cos(theta)
                //Law of Cosines: A = 2*B*cos(theta)
                //Law of Cosines: A/(2*cos(theta)) = B
                //Law of Cosines: 0.5f*A/cos(theta) = B
                //Law of Cosines: 0.5f * targetToMuzzleDist / cos(theta) = targetSpeed * t
                //We know that cos(theta) of zero or less means there is no solution, since that would mean B goes backwards or leads to div/0 (infinity)
                if (cosTheta > 0) {
                    t = 0.5f * targetToMuzzleDist / (targetSpeed * cosTheta);
                }
                else {
                    validSolutionFound = false;
                    t = PredictiveAimWildGuessAtImpactTime();
                }
            }
            else {
                //Quadratic formula: Note that lower case 'a' is a completely different derived variable from capital 'A' used in Law of Cosines (sorry):
                //t = [ -b � Sqrt( b*b - 4*a*c ) ] / (2*a)
                float a = projectileSpeedSq - targetSpeedSq;
                float b = 2.0f * targetToMuzzleDist * targetSpeed * cosTheta;
                float c = -targetToMuzzleDistSq;
                float discriminant = b * b - 4.0f * a * c;

                if (discriminant < 0) {
                    //Square root of a negative number is an imaginary number (NaN)
                    //Special thanks to Rupert Key (Twitter: @Arakade) for exposing NaN values that occur when target speed is faster than or equal to projectile speed
                    validSolutionFound = false;
                    t = PredictiveAimWildGuessAtImpactTime();
                }
                else {
                    //a will never be zero because we protect against that with "if (Mathf.Approximately(projectileSpeedSq, targetSpeedSq))" above
                    float uglyNumber = Mathf.Sqrt(discriminant);
                    float t0 = 0.5f * (-b + uglyNumber) / a;
                    float t1 = 0.5f * (-b - uglyNumber) / a;
                    //Assign the lowest positive time to t to aim at the earliest hit
                    t = Mathf.Min(t0, t1);
                    if (t < Mathf.Epsilon) {
                        t = Mathf.Max(t0, t1);
                    }

                    if (t < Mathf.Epsilon) {
                        //Time can't flow backwards when it comes to aiming.
                        //No real solution was found, take a wild shot at the target's future location
                        validSolutionFound = false;
                        projectileVelocity = Vector3.zero;
                        projectileDestination = muzzlePosition;
                        return false;
                        //t = PredictiveAimWildGuessAtImpactTime();
                    }
                }
            }

            //Vb = Vt - 0.5*Ab*t + [(Pti - Pbi) / t]
            projectileVelocity = targetVelocity + (-targetToMuzzle / t);
            if (!validSolutionFound) {
                //PredictiveAimWildGuessAtImpactTime gives you a t that will not result in impact
                // Which means that all that math that assumes projectileSpeed is enough to impact at time t breaks down
                // In this case, we simply want the direction to shoot to make sure we
                // don't break the gameplay rules of the cannon's capabilities aside from gravity compensation
                projectileVelocity = projectileSpeed * projectileVelocity.normalized;
            }

            if (!Mathf.Approximately(gravity, 0)) {
                //By adding gravity as projectile acceleration, we are essentially breaking real world rules by saying that the projectile
                // gets any upwards/downwards gravity compensation velocity for free, since the projectileSpeed passed in is a constant that assumes zero gravity
                Vector3 projectileAcceleration = gravity * Vector3.down;
                //assuming gravity is a positive number, this next line will apply a free magical upwards lift to compensate for gravity
                Vector3 gravityCompensation = (0.5f * projectileAcceleration * t);
                //Let's cap gravityCompensation to avoid AIs that shoot infinitely high
                float gravityCompensationCap = 0.5f * projectileSpeed;  //let's assume we won't lob higher than 50% of the canon's shot range
                if (gravityCompensation.magnitude > gravityCompensationCap) {
                    gravityCompensation = gravityCompensationCap * gravityCompensation.normalized;
                }
                projectileVelocity -= gravityCompensation;
            }

            //FOR CHECKING ONLY (valid only if gravity is 0)...
            //float calculatedprojectilespeed = projectileVelocity.magnitude;
            //bool projectilespeedmatchesexpectations = (projectileSpeed == calculatedprojectilespeed);
            //...FOR CHECKING ONLY
            projectileDestination = targetPosition + t * targetVelocity;
            return validSolutionFound;
        }

    }
}