using System.Collections;
using System.Collections.Generic;
using Andja.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GrowableStructureTest {

    GrowableStructure growable;

    [SetUp]
    public void SetUp() {
        GrowablePrototypeData growablePrototypeData = new GrowablePrototypeData {
            ageStages = 2,
            produceTime = 1,
        };
        growable = new GrowableStructure("growable", growablePrototypeData);

    }

}
