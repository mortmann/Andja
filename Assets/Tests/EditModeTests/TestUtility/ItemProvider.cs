using Andja.Model;
public class ItemProvider  {
    public static Item Wood => new Item("Wood");
    public static Item Wood_1 => Wood_N(1);
    public static Item Wood_10 => Wood_N(10);
    public static Item Wood_5 => Wood_N(5);
    public static Item Wood_50 => Wood_N(50);
    public static Item Wood_100 => Wood_N(100);
    public static Item Wood_N(int n) => new Item("Wood", n);
    public static Item Tool => new Item("Tool");
    public static Item Tool_5 => new Item("Tool", 5);
    public static Item Tool_12 => new Item("Tool", 12);

    public static Item Stone => new Item("Stone");
    public static Item Stone_1 => Stone_N(1);
    public static Item Stone_25 => Stone_N(25);
    public static Item Stone_N(int n) => new Item("Stone", n);
    
    public static Item Brick => new Item("Brick");
    public static Item Brick_25=> new Item("Brick", 25);

    public static Item Fish => new Item("Fish");
    public static Item Fish_1 => new Item("Fish", 1);
    public static Item Fish_2 => new Item("Fish", 2);
    public static Item Fish_25 => new Item("Fish", 25);

}
