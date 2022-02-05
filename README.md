<div align="center">
  <a href="https://github.com/mortmann/Andja">
    <img src="Assets\Resources\UI Images\temporary_exe_icon.png" alt="Logo" width="80" height="80">
  </a>

<h1 align="center">Andja</h1>
<h3 align="center">A city-building game with resource management and supply chain mechanics.</h3>
<h4 align="center">In alpha and is currently not meant to be played. Features still in development.<h4>
</div>
<br>
<br>

## About The Project

Explore maps and settle on islands. Build cities and production chains to provide the population with the desired items. 

Produce the items via supply chains, which grow in complexity with each new unlocked item. 

Settle on new islands to acquire the required resources and fertilities for each new chain. Manage trade routes for efficient supply chain management.

Make money through taxes and other means, like selling items to traders, the off-world market, or AI players.

Explore randomly generated maps with either procedurally generated islands or hand-designed ones. 

Compete with AI players for limited island space and resources. 


## Features

* Virtually unlimited maps and islands
  *  Due to random generation
  *  Islands can be created manuelly in the ingame editor as well   
  
* Model–view–controller
  *  Graphics and models are mostly seperated, which would allow a switch to 3D-Graphics 
  *  Except the collision handling

* Data loaded from XML-Files
  *  Easily modify data to balance or create new structures, units and other ingame objects
  *  Each time the game loads a savefile, the XML-files are reloaded, which means fast testing of balance changes
  *  <a href="https://github.com/mortmann/AndjaXMLCreator">Dedicated program to easily edit all data</a>

* Lots of content (more in development) (State 03.02.2022)
  *  120 structures
  *  89 items
  *  38 needs in 9 different groups
  *  12 fertilities in 3 island types (warm, cold and temperate) 
  *  4 population levels
  *  and more 
  
* Data saved in JSON-Files
  *  Allows for easy analysis of savegames
  *  Fast and easy to save and read

* Supports different languages
  *  Change at runtime

* Mods
  *  Everybody can create extra content for all types of objects
  *  Island generation can be modified with LUA functions
  *  Select in main menu which ones are enabled
  *  Each savegame can have their own active mods
  
* Options menu
  *  Change the game to your preferences 

* Ingame console
  *  Use commands to change the game or activate debug informations
  *  See errors and other informations

* Bug-Report-Tool
  *  Report bugs directly ingame
  *  Upload the corresponding savefile, logfile and up to 3 screenshots 
  *  Connected to JetBrains YouTrack for easy management

* Ingame news
  *  Read the newest information in the main menu
  *  Uses Pastebin.com for now, but can easily changed to a self-hosted server

* Pathfinding
  *  Uses A* search algorithm
  *  Multithreaded for better performance
  
* AI Players
  *  Build their own cities (for now only basic placement)
  *  Diplomacy will be added later 
  *  Multithreaded, so each AI can calulate at the same time, what to do next

* 3 different Fog-of-War options
  *  Off: Everything is always visible
  *  Unkown: Hides map until the player explores it
  *  Always: Like 'Unkown' but also hides activities that is not in the player's view (either settled islands or unit range)

## Code Statistics for Assets\Scripts: State 05.02.2022
````
C# classes        :       373
C# interfaces     :         5
C# structs        :        11
C# enums          :        88
C# functions      :     2.871
C# properties     :       693
  
Total C# lines    :    43.989
C# comment lines  :     2.736
C# empty lines    :     4.768
C# pure code lines:    36.485
````
	

## Built With
* [Unity Engine](https://unity.com/) and Asset Store:
  *  [AdvancedPolygonCollider](https://assetstore.unity.com/packages/tools/physics/advanced-polygon-collider-52265)
  *  [Graphy](https://assetstore.unity.com/packages/tools/gui/graphy-ultimate-fps-counter-stats-monitor-debugger-105778)
* [Cloud Shadow](https://github.com/EntroPi-Games/Unity-Cloud-Shadows/)
* [MoonSharp.Interpreter](https://www.moonsharp.org/)
* [Newtonsoft JOSN](https://www.newtonsoft.com/json)
* [Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)  
* [FastNoise](https://github.com/Auburns/FastNoise)
* [DotNetZip](https://github.com/DinoChiesa/DotNetZip)
* [SimplePool](https://gist.github.com/quill18/5a7cfffae68892621267)
* [Easing Functions](https://gist.github.com/cjddmut/d789b9eb78216998e95c)

## Disclaimer
* Nothing is final and everything will change
* Graphics are 2D and mostly programmers art and therefore placeholders!
* Same goes for the audio and music tracks!
* UI will be reworked
* AI currently active under development and does only basic things
