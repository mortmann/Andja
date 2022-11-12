using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Andja.Controller;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class CityTest {

    City PlayerCity;
    MockUtil MockUtil;
    [SetUp]
    public void SetUp() {
        MockUtil = new MockUtil().WithOnePopulationLevels();
        PlayerCity = new City(0, MockUtil.IslandMock.Object);
    }

}