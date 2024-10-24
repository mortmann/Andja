﻿/*
 * Copyright (c) 2015 Thomas Hourdel
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */

using UnityEngine;

namespace Smaa {
    /// <summary>
    /// Attribute used to make a float or int variable in a script be restricted to a specific minimum value.
    /// </summary>
    public sealed class MinAttribute : PropertyAttribute {
        /// <summary>
        /// The minimum value to clamp to.
        /// </summary>
        public readonly float min;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="min">The minimum value to clamp to.</param>
        public MinAttribute(float min) {
            this.min = min;
        }
    }
}
