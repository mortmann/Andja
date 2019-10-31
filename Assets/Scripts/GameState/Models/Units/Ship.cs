using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ShipPrototypeData : UnitPrototypeData {
    public int maximumAmountOfCannons = 0;
    public int damagePerCannon = 1;
    public float length;
    public float cannonSpeedDebuffMultiplier=0.1f;
    public float inventorySpeedDebuffMultiplier = 0.15f;
    public float damageSpeedDebuffMultiplier = 0.7f;
    //TODO: think about a way it doesnt require each ship to have this 
    //      OR are there sometyp like HEAVY and prototype returns the associated cannon
    public Item cannonType = null;
}

//TODO: think about how if ships could be capturable if they are low, at war and the capturing ship can do it?
[JsonObject(MemberSerialization.OptIn)]
public class Ship : Unit {
    float ProjectileSpeed => ShipData.projectileSpeed; //TODO: somewhere you gotta read this in


    [JsonPropertyAttribute] public TradeRoute tradeRoute;
    [JsonPropertyAttribute] public bool isOffWorld;
    [JsonPropertyAttribute] Item[] toBuy;
    [JsonPropertyAttribute] float offWorldTime;
    [JsonPropertyAttribute] Item _cannonItem;
    public Item CannonItem {
        get { if (_cannonItem == null) { _cannonItem = ShipData.cannonType.CloneWithCount(); } return _cannonItem; }
    }
    protected ShipPrototypeData _shipPrototypData;

    public int DamagePerCannon => CalculateRealValue("damagePerCannon", ShipData.damagePerCannon);
    public int MaximumAmountOfCannons => CalculateRealValue("maximumAmountOfCannons", ShipData.maximumAmountOfCannons);
    public override float CurrentDamage => CalculateRealValue("CurrentDamage", DamagePerCannon);
    public override float MaximumDamage => CalculateRealValue("MaximumDamage", MaximumAmountOfCannons * DamagePerCannon);
    public override bool IsShip => true;
    public override float SpeedModifier => 1 - CannonSpeedDebuff - InventorySpeedDebuff - DamageSpeedDebuff;
    protected float CannonSpeedDebuff => MaximumAmountOfCannons == 0? 0 : ShipData.cannonSpeedDebuffMultiplier * (CannonItem.count / (float)MaximumAmountOfCannons);
    protected float InventorySpeedDebuff => ShipData.inventorySpeedDebuffMultiplier * inventory.GetFilledPercantage();
    protected float DamageSpeedDebuff => ShipData.damageSpeedDebuffMultiplier * (1 - CurrentHealth/MaximumHealth);

    protected int CannonPerSide => Mathf.CeilToInt(CannonItem.count / 2);
     
    public ShipPrototypeData ShipData {
        get {
            if (_shipPrototypData == null) {
                _shipPrototypData = (ShipPrototypeData)PrototypController.Instance.GetUnitPrototypDataForID(ID);
            }
            return _shipPrototypData;
        }
    }
    public Ship() {
        
    }
    public Ship(Tile t, int playernumber) {
        this.playerNumber = playernumber;
        inventory = new Inventory(6, 50);
        offWorldTime = 5f;
        pathfinding = new OceanPathfinding(t, this);
        patrolCommand = new PatrolCommand();
    }
    public Ship(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        patrolCommand = new PatrolCommand();

        this._prototypData = unit.Data;
        this.CurrentHealth = MaxHealth;
        this.playerNumber = playerNumber;
        inventory = new Inventory(InventoryPlaces, InventorySize);
        PlayerSetName = "Ship " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new OceanPathfinding(t, this);
    }
    public override Unit Clone(int playerNumber, Tile t) {
        return new Ship(this, playerNumber, t);
    }
    public Ship(string id, ShipPrototypeData spd) {
        this.ID = id;
        this._shipPrototypData = spd;
    }

    public override void DoAttack(float deltaTime) {
        if (CurrentTarget != null) {
            if (attackCooldownTimer > 0) {
                attackCooldownTimer -= deltaTime;
                return;
            }
            Vector3 position = CurrentPosition;
            Vector3 velocity = new Vector3();
            Vector3 targetPosition = CurrentTarget.CurrentPosition;
            Vector3 lastMove = CurrentTarget.LastMovement;
            Vector3 projectileDestination = CurrentTarget.CurrentPosition;
            if (PredictiveAim( CurrentPosition, ProjectileSpeed, targetPosition, lastMove, 0, out velocity, out projectileDestination) ==false) {
                return;
            }
            ShotAtPosition(projectileDestination);
            //float distance = (projectileDestination - VectorPosition).magnitude;
            //for (int i = 1; i <= CannonPerSide; i++) {
            //    Vector3 offset = new Vector3(Width / 2, (i) * (Height / MaximumAmountOfCannons));
            //    offset = Quaternion.Euler(0, 0, pathfinding.rotation) * offset;
            //    cbCreateProjectile?.Invoke(new Projectile(this, position + offset, CurrentTarget, velocity, distance));
            //}
        }
    }

    public override bool IsInRange() {
        if (CurrentTarget == null)
            return false;
        if(CurrentTarget.LastMovement.sqrMagnitude==0)
            return (CurrentTarget.CurrentPosition - CurrentPosition).magnitude <= AttackRange;
        Vector3 position = CurrentPosition;
        Vector3 velocity = new Vector3();
        Vector3 targetPosition = CurrentTarget.CurrentPosition;
        Vector3 lastMove = CurrentTarget.LastMovement;
        Vector3 projectileDestination = CurrentTarget.CurrentPosition;
        bool can = PredictiveAim(CurrentPosition, ProjectileSpeed, targetPosition, lastMove, 0, out velocity, out projectileDestination);
        if (can == false && Vector3.Distance(CurrentPosition, projectileDestination) > AttackRange)
            return false;
        nextShoot = CalculateShootAngle(projectileDestination);
        float rotateTime = CalculateRotateTime(nextShoot.rotateAngle);
        targetPosition += rotateTime * lastMove;
        can = PredictiveAim(CurrentPosition, ProjectileSpeed, targetPosition, lastMove, 0, out velocity, out projectileDestination);
        if (can == false && Vector3.Distance(CurrentPosition, projectileDestination) > AttackRange)
            return false;
        nextShoot = CalculateShootAngle(projectileDestination);

        return true;
    }
    //TODO: think about making it like this?
    //calculate in the check range and if in range and possible then just do the shoot calculate there?
    Shoot nextShoot;
    public void ShotAtPosition(Vector3 destination) {
        float arc = 30f;
        Vector3 targetSize = new Vector3(1, 1, 0);
        Vector3 position = CurrentPosition;
        Vector2 forward = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(1, 0);
        Vector2 direction = destination - position;
        float sideAngle = Vector2.SignedAngle(direction, forward);
        Vector2 side;

        float widthOffset = 0;
        if(sideAngle<0) {
            side = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(0, 1);
            widthOffset = Width / 2;
        }
        else {
            side = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(0, -1);
            widthOffset = - Width / 2;
        }
        float shootAngle = Vector2.SignedAngle(direction, side);
        bool canShoot = shootAngle <= arc && shootAngle >= -arc;
        if (canShoot == false) 
            pathfinding.Rotate(-shootAngle);
        if (attackCooldownTimer > 0) {
            return;
        }
        for (int i = 1; i <= CannonPerSide; i++) {
            Vector3 offset = new Vector3( (i) * (Height / MaximumAmountOfCannons) - Height/2, widthOffset);
            offset = Quaternion.Euler(0, 0, Rotation) * offset;
            Vector3 targetOffset = new Vector3(
                    UnityEngine.Random.Range(-targetSize.x / 2, targetSize.x / 2),
                    UnityEngine.Random.Range(-targetSize.y / 2, targetSize.y / 2),
                    UnityEngine.Random.Range(-targetSize.z / 2, targetSize.z / 2));

            Vector3 velocity = (destination + targetOffset - VectorPosition - offset).normalized * ProjectileSpeed;
            float distance = (destination+ targetOffset - VectorPosition - offset).magnitude;
            cbCreateProjectile?.Invoke(new Projectile(this, position+offset, CurrentTarget, velocity, distance));
        }
        attackCooldownTimer = AttackRate;
    }
    protected Shoot CalculateShootAngle(Vector3 destination) {
        Vector3 position = CurrentPosition;
        Vector2 forward = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(1, 0);
        Vector2 direction = destination - position;
        float sideAngle = Vector2.SignedAngle(direction, forward);
        Vector2 side;

        if (sideAngle < 0) {
            side = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(0, 1);
        }
        else {
            side = Quaternion.Euler(0, 0, pathfinding.rotation) * new Vector2(0, -1);
        }
        float shootAngle = Vector2.SignedAngle(direction, side);
        return new Shoot {
            sideAngle = sideAngle,
            rotateAngle = shootAngle
        };
    }
    public float CalculateRotateTime(float angle) {
        return angle / RotationSpeed;
    }

    protected override void UpdateTradeRoute(float deltaTime) {
        if (tradeRoute == null || tradeRoute.Valid == false) {
            CurrentMainMode = UnitMainModes.Idle;
            return;
        }
        if (pathfinding.IsAtDestination) {
            //do trading here
            //take some time todo that
            if (tradeTime > 0) {
                tradeTime -= deltaTime;
                return;
            }
            tradeRoute.DoCurrentTrade(this);
            tradeTime = 1.5f;
            SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
        }
    }
    public void SetTradeRoute(TradeRoute tr) {
        tradeRoute = tr;
        StartTradeRoute();
    }

    public void StartTradeRoute() {
        if (tradeRoute == null)
            return;
        CurrentMainMode = UnitMainModes.TradeRoute;
        SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
    }

    private void SetDestinationIfPossible(Tile tile) {
        SetDestinationIfPossible(tile.X, tile.Y);
    }

    internal bool HasCannonsToAddInInventory() {
        return inventory.ContainsItemWithID(CannonItem.ID);
    }

    protected override void UpdateWorldMarket(float deltaTime) {
        if (pathfinding.IsAtDestination && isOffWorld == false) {
            isOffWorld = true;
            CallChangedCallback();
        }
        if (offWorldTime > 0) {
            offWorldTime -= deltaTime;
            return;
        }
        offWorldTime = 3;
        OffworldMarket om = WorldController.Instance.offworldMarket;
        //FIRST SELL everything in inventory to make space for all the things
        Player myPlayer = PlayerController.GetPlayer(playerNumber);
        Item[] i = inventory.GetAllItemsAndRemoveThem();
        foreach (Item item in i) {
            om.SellItemToOffWorldMarket(item, myPlayer);
        }
        foreach (Item item in toBuy) {
            inventory.AddItem(om.BuyItemToOffWorldMarket(item, item.count, myPlayer));
        }
        isOffWorld = false;
        CurrentMainMode = UnitMainModes.Idle;
        CallChangedCallback();
    }
    /// <summary>
    /// Does not remove itself from TradeRoute 
    /// Instead call it from the TradeRoute -> RemoveShip()!
    /// </summary>
    internal void StopTradeRoute() {
        tradeRoute = null;
        CurrentMainMode = UnitMainModes.Idle;
    }

    internal void RemoveCannonsToInventory(bool all) {
        if(all)
            CannonItem.count -= inventory.AddItem(CannonItem);
        else {
            Item temp = CannonItem.Clone();
            temp.count = 1;
            CannonItem.count -= inventory.AddItem(temp);
        }
    }

    internal void AddCannonsFromInventory(bool all) {
        if (all) {
            Item temp = CannonItem.Clone();
            temp.count = MaximumAmountOfCannons - CannonItem.count;
            CannonItem.count += inventory.GetItemWithMaxItemCount(temp).count;
        }
        else {
            Item temp = CannonItem.Clone();
            temp.count = Mathf.Min(1,MaximumAmountOfCannons - CannonItem.count);
            CannonItem.count += inventory.GetItemWithMaxItemCount(temp).count;
        }
    }

    internal bool CanRemoveCannons() {
        if (CannonItem.count <= 0) {
            return false;
        }
        if (inventory.HasSpaceForItem(CannonItem) == false) {
            return false;
        }
        return true;
    }

    public void SendToOffworldMarket(Item[] toBuy) {
        //TODO OPTIMISE THIS SO IT CHECKS THE ROUTE FOR ANY
        //ISLANDS SO IT CAN TAKE A OTHER ROUTE
        if (X >= Y) {
            SetDestinationIfPossible(0, Y);
        }
        if (X < Y) {
            SetDestinationIfPossible(X, 0);
        }
        this.toBuy = toBuy;
        CurrentMainMode = UnitMainModes.OffWorldMarket;
    }
    /// <summary>
    /// Returns true only if it can reach the exact tile but
    /// will try still to get close as possible to the given coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
	protected override bool SetDestinationIfPossible(float x, float y) {
        Tile tile = World.Current.GetTileAt(x, y);
        if (tile == null) {
            return false;
        }
        ((OceanPathfinding)pathfinding).SetDestination(x, y);
        CurrentDoingMode = UnitDoModes.Move;
        return tile.Type == TileType.Ocean;
    }
    /// <summary>
    /// Returns the added amount of cannons
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public void AddCannons(Item toAdd) {
        if (toAdd.ID != CannonItem.ID) {
            Debug.LogWarning("Tried to add incombatible cannons to this ship!");
            return;
        }
        int restneeded = ShipData.maximumAmountOfCannons - CannonItem.count;
        int added = Mathf.Clamp(toAdd.count, 0, restneeded);
        CannonItem.count += added;
        toAdd.count -= added;
    }
    public override float GetCurrentDamage(Combat.ArmorType armorType) {
        return MyDamageType.GetDamageMultiplier(armorType) * ShipData.damagePerCannon;
    }

    //////////////////////////////////////////////////////////////////////////////
    //This implies that no solution exists for this situation as the target may literally outrun the projectile with its current direction
    //In cases like that, we simply aim at the place where the target will be 1 to 5 seconds from now.
    //Feel free to randomize t at your discretion for your specific game situation if you want that guess to feel appropriately noisier
    static float PredictiveAimWildGuessAtImpactTime() {
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
    protected struct Shoot {
        public float rotateAngle;
        public float sideAngle;
    }
}