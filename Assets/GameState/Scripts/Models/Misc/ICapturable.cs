using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICapturable : ITargetable {
    void Capture(IWarfare warfare, float progress);
    bool Captured { get; }

}
