// TextToTexture - Class for apply Text-To-Texture without need for Render Texture
//
// released under MIT License
// http://www.opensource.org/licenses/mit-license.php
//
//@author		Devin Reimer
//@version		1.0.0
//@website 		http://blog.almostlogical.com

//Copyright (c) 2010 Devin Reimer
/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextToTexture {
    private const int ASCII_START_OFFSET = 32;
    private Font customFont;
    private Texture2D fontTexture;
    private int fontCountX;
    private int fontCountY;
    private float[] kerningValues;
    private bool supportSpecialCharacters;

    public TextToTexture(Font customFont, int fontCountX, int fontCountY, bool supportSpecialCharacters, PerCharacterKerning[] perCharacterKerning=null) {
        this.customFont = customFont;
        fontTexture = (Texture2D)customFont.material.mainTexture;
        this.fontCountX = fontCountX;
        this.fontCountY = fontCountY;
        if(perCharacterKerning==null) {
            kerningValues = DefaultArialCharacterKerning();
        } else {
            kerningValues = GetCharacterKerningValuesFromPerCharacterKerning(perCharacterKerning);
        }
        this.supportSpecialCharacters = supportSpecialCharacters;
    }

    //placementX and Y - placement within texture size, texture size = textureWidth and textureHeight (square)
    public Texture2D CreateTextToTexture(string text, int textPlacementX, int textPlacementY, int textureSize, float characterSize, float lineSpacing) {
        Texture2D txtTexture = CreatefillTexture2D(Color.clear, textureSize, textureSize);
        int fontGridCellWidth = (int)(fontTexture.width / fontCountX);
        int fontGridCellHeight = (int)(fontTexture.height / fontCountY);
        int fontItemWidth = (int)(fontGridCellWidth * characterSize);
        int fontItemHeight = (int)(fontGridCellHeight * characterSize);
        Vector2 charTexturePos;
        Color[] charPixels;
        float textPosX = textPlacementX;
        float textPosY = textPlacementY;
        float charKerning;
        bool nextCharacterSpecial = false;
        char letter;

        for (int n = 0; n < text.Length; n++) {
            letter = text[n];
            nextCharacterSpecial = false;

            if (letter == '\\' && supportSpecialCharacters) {
                nextCharacterSpecial = true;
                if (n + 1 < text.Length) {
                    n++;
                    letter = text[n];
                    if (letter == 'n' || letter == 'r') //new line or return
                    {
                        textPosY -= fontItemHeight * lineSpacing;
                        textPosX = textPlacementX;
                    }
                    else if (letter == 't') {
                        textPosX += fontItemWidth * GetKerningValue(' ') * 5; //5 spaces
                    }
                    else if (letter == '\\') {
                        nextCharacterSpecial = false; //this allows for printing of \
                    }
                }
            }

            if (!nextCharacterSpecial && customFont.HasCharacter(letter)) {
                charTexturePos = GetCharacterGridPosition(letter);
                charTexturePos.x *= fontGridCellWidth;
                charTexturePos.y *= fontGridCellHeight;
                charPixels = fontTexture.GetPixels((int)charTexturePos.x, fontTexture.height - (int)charTexturePos.y - fontGridCellHeight, fontGridCellWidth, fontGridCellHeight);
                charPixels = changeDimensions(charPixels, fontGridCellWidth, fontGridCellHeight, fontItemWidth, fontItemHeight);

                txtTexture = AddPixelsToTextureIfClear(txtTexture, charPixels, (int)textPosX, (int)textPosY, fontItemWidth, fontItemHeight);
                charKerning = GetKerningValue(letter);
                textPosX += (fontItemWidth * charKerning); //add kerning here
            }
            else if (!nextCharacterSpecial) {
                Debug.Log("Letter Not Found:" + letter);
            }

        }
        txtTexture.Apply();
        return txtTexture;
    }

    //doesn't yet support special characters
    //trailing buffer is to allow for area where the character might be at the end
    public int CalcTextWidthPlusTrailingBuffer(string text, int decalTextureSize, float characterSize) {
        char letter;
        float width = 0;
        int fontItemWidth = (int)((fontTexture.width / fontCountX) * characterSize);

        for (int n = 0; n < text.Length; n++) {
            letter = text[n];
            if (n < text.Length - 1) {
                width += fontItemWidth * GetKerningValue(letter);
            }
            else //last letter ignore kerning for buffer
            {
                width += fontItemWidth;
            }
        }

        return (int)width;
    }

    //look for a faster way of calculating this
    private Color[] changeDimensions(Color[] originalColors, int originalWidth, int originalHeight, int newWidth, int newHeight) {
        Color[] newColors;
        Texture2D originalTexture;
        int pixelCount;
        float u;
        float v;

        if (originalWidth == newWidth && originalHeight == newHeight) {
            newColors = originalColors;
        }
        else {
            newColors = new Color[newWidth * newHeight];
            originalTexture = new Texture2D(originalWidth, originalHeight);

            originalTexture.SetPixels(originalColors);
            for (int y = 0; y < newHeight; y++) {
                for (int x = 0; x < newWidth; x++) {
                    pixelCount = x + (y * newWidth);
                    u = (float)x / newWidth;
                    v = (float)y / newHeight;
                    newColors[pixelCount] = originalTexture.GetPixelBilinear(u, v);
                }
            }
        }

        return newColors;
    }

    //add pixels to texture if pixels are currently clear
    private Texture2D AddPixelsToTextureIfClear(Texture2D texture, Color[] newPixels, int placementX, int placementY, int placementWidth, int placementHeight) {
        int pixelCount = 0;
        Color[] currPixels;

        if (placementX + placementWidth < texture.width) {
            currPixels = texture.GetPixels(placementX, placementY, placementWidth, placementHeight);

            for (int y = 0; y < placementHeight; y++) //vert
            {
                for (int x = 0; x < placementWidth; x++) //horiz
                {
                    pixelCount = x + (y * placementWidth);
                    if (currPixels[pixelCount] != Color.clear) //if pixel is not empty take the previous value
                    {
                        newPixels[pixelCount] = currPixels[pixelCount];
                    }
                }
            }

            texture.SetPixels(placementX, placementY, placementWidth, placementHeight, newPixels);
        }
        else {
            Debug.Log("Letter Falls Outside Bounds of Texture" + (placementX + placementWidth));
        }
        return texture;
    }

    private Vector2 GetCharacterGridPosition(char c) {
        int codeOffset = c - ASCII_START_OFFSET;

        return new Vector2(codeOffset % fontCountX, (int)codeOffset / fontCountX);
    }

    private float GetKerningValue(char c) {
        return kerningValues[((int)c) - ASCII_START_OFFSET];
    }

    private Texture2D CreatefillTexture2D(Color color, int textureWidth, int textureHeight) {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        int numOfPixels = texture.width * texture.height;
        Color[] colors = new Color[numOfPixels];
        for (int x = 0; x < numOfPixels; x++) {
            colors[x] = color;
        }

        texture.SetPixels(colors);

        return texture;
    }

    private float[] GetCharacterKerningValuesFromPerCharacterKerning(PerCharacterKerning[] perCharacterKerning) {
        float[] perCharKerning = new float[128 - ASCII_START_OFFSET];
        int charCode;

        foreach (PerCharacterKerning perCharKerningObj in perCharacterKerning) {
            if (perCharKerningObj.First != "") {
                charCode = (int)perCharKerningObj.GetChar(); ;
                if (charCode >= 0 && charCode - ASCII_START_OFFSET < perCharKerning.Length) {
                    perCharKerning[charCode - ASCII_START_OFFSET] = perCharKerningObj.GetKerningValue();
                }
            }
        }
        return perCharKerning;
    }

    //default is arial, this can be changed so you don't have to go through the painful process of adding kerning through the editor
    public static float[] DefaultArialCharacterKerning() {
        double[] perCharKerningDouble = new double[] {
 .201 /* */
,.201 /*!*/
,.256 /*"*/
,.401 /*#*/
,.401 /*$*/
,.641 /*%*/
,.481 /*&*/
,.138 /*'*/
,.24 /*(*/
,.24 /*)*/
,.281 /***/
,.421 /*+*/
,.201 /*,*/
,.24 /*-*/
,.201 /*.*/
,.201 /*/*/
,.401 /*0*/
,.353 /*1*/
,.401 /*2*/
,.401 /*3*/
,.401 /*4*/
,.401 /*5*/
,.401 /*6*/
,.401 /*7*/
,.401 /*8*/
,.401 /*9*/
,.201 /*:*/
,.201 /*;*/
,.421 /*<*/
,.421 /*=*/
,.421 /*>*/
,.401 /*?*/
,.731 /*@*/
,.481 /*A*/
,.481 /*B*/
,.52  /*C*/
,.481 /*D*/
,.481 /*E*/
,.44  /*F*/
,.561 /*G*/
,.52  /*H*/
,.201 /*I*/
,.36  /*J*/
,.481 /*K*/
,.401 /*L*/
,.6   /*M*/
,.52  /*N*/
,.561 /*O*/
,.481 /*P*/
,.561 /*Q*/
,.52  /*R*/
,.481 /*S*/
,.44  /*T*/
,.52  /*U*/
,.481 /*V*/
,.68  /*W*/
,.481 /*X*/
,.481 /*Y*/
,.44  /*Z*/
,.201 /*[*/
,.201 /*\*/
,.201 /*]*/
,.338 /*^*/
,.401 /*_*/
,.24  /*`*/
,.401 /*a*/
,.401 /*b*/
,.36  /*c*/
,.401 /*d*/
,.401 /*e*/
,.189 /*f*/
,.401 /*g*/
,.401 /*h*/
,.16  /*i*/
,.16  /*j*/
,.36  /*k*/
,.16  /*l*/
,.6   /*m*/
,.401 /*n*/
,.401 /*o*/
,.401 /*p*/
,.401 /*q*/
,.24  /*r*/
,.36  /*s*/
,.201 /*t*/
,.401 /*u*/
,.36  /*v*/
,.52  /*w*/
,.36  /*x*/
,.36  /*y*/
,.36  /*z*/
,.241 /*{*/
,.188 /*|*/
,.241 /*}*/
,.421 /*~*/
};
        float[] perCharKerning = new float[perCharKerningDouble.Length];

        for (int x = 0; x < perCharKerning.Length; x++) {
            perCharKerning[x] = (float)perCharKerningDouble[x];
        }
        return perCharKerning;
    }
}

//   //default is arial, this can be changed so you don't have to go through the painful process of adding kerning through the editor
//    public static PerCharacterKerning[] DefaultArialCharacterKerning() {
//        PerCharacterKerning[] perCharKerningDouble = new PerCharacterKerning[] {
// new PerCharacterKerning(" ", .201f) /* */
//,new PerCharacterKerning("!", .201f) /*!*/
//,new PerCharacterKerning("\"", .256f) /*"*/
//,new PerCharacterKerning("#", .401f) /*#*/
//,new PerCharacterKerning("$", .401f) /*$*/
//,new PerCharacterKerning("%", .641f) /*%*/
//,new PerCharacterKerning("&", .481f) /*&*/
//,new PerCharacterKerning("'", .138f) /*'*/
//,new PerCharacterKerning("(", .24f) /*(*/
//,new PerCharacterKerning(")", .24f) /*)*/
//,new PerCharacterKerning("*", .281f) /***/
//,new PerCharacterKerning("+", .421f) /*+*/
//,new PerCharacterKerning(",", .201f) /*,*/
//,new PerCharacterKerning("-", .24f) /*-*/
//,new PerCharacterKerning(".", .256f).201 /*.*/
//,new PerCharacterKerning("/", .256f).201 /*/*/
//,new PerCharacterKerning("0", .256f).401 /*0*/
//,new PerCharacterKerning("1", .256f).353 /*1*/
//,new PerCharacterKerning("2", .256f).401 /*2*/
//,new PerCharacterKerning("3", .256f).401 /*3*/
//,new PerCharacterKerning("4", .256f).401 /*4*/
//,new PerCharacterKerning("5", .256f).401 /*5*/
//,new PerCharacterKerning("6", .256f).401 /*6*/
//,new PerCharacterKerning("7", .256f).401 /*7*/
//,new PerCharacterKerning("8", .256f).401 /*8*/
//,new PerCharacterKerning("9", .256f).401 /*9*/
//,new PerCharacterKerning(":", .256f).201 /*:*/
//,new PerCharacterKerning(";", .256f).201 /*;*/
//,new PerCharacterKerning("<", .256f).421 /*<*/
//,new PerCharacterKerning("=", .256f).421 /*=*/
//,new PerCharacterKerning(">", .256f).421 /*>*/
//,new PerCharacterKerning("?", .256f).401 /*?*/
//,new PerCharacterKerning("@", .256f).731 /*@*/
//,new PerCharacterKerning("A", .256f).481 /*A*/
//,new PerCharacterKerning("B", .256f).481 /*B*/
//,new PerCharacterKerning("C", .256f).52  /*C*/
//,new PerCharacterKerning("D", .256f).481 /*D*/
//,new PerCharacterKerning("E", .256f).481 /*E*/
//,new PerCharacterKerning("F", .256f).44  /*F*/
//,new PerCharacterKerning("G", .256f).561 /*G*/
//,new PerCharacterKerning("H", .256f).52  /*H*/
//,new PerCharacterKerning("I", .256f).201 /*I*/
//,new PerCharacterKerning("J", .256f).36  /*J*/
//,new PerCharacterKerning("K", .256f).481 /*K*/
//,new PerCharacterKerning("L", .256f).401 /*L*/
//,new PerCharacterKerning("M", .256f).6   /*M*/
//,new PerCharacterKerning("N", .256f).52  /*N*/
//,new PerCharacterKerning("O", .256f).561 /*O*/
//,new PerCharacterKerning("P", .256f).481 /*P*/
//,new PerCharacterKerning("Q", .256f).561 /*Q*/
//,new PerCharacterKerning("R", .256f).52  /*R*/
//,new PerCharacterKerning("S", .256f).481 /*S*/
//,new PerCharacterKerning("T", .256f).44  /*T*/
//,new PerCharacterKerning("U", .256f).52  /*U*/
//,new PerCharacterKerning("V", .256f).481 /*V*/
//,new PerCharacterKerning("W", .256f).68  /*W*/
//,new PerCharacterKerning("X", .256f).481 /*X*/
//,new PerCharacterKerning("Y", .256f).481 /*Y*/
//,new PerCharacterKerning("Z", .256f).44  /*Z*/
//,new PerCharacterKerning("[", .256f).201 /*[*/
//,new PerCharacterKerning("\\", .256f).201 /*\*/
//,new PerCharacterKerning("]", .256f).201 /*]*/
//,new PerCharacterKerning("^", .256f).338 /*^*/
//,new PerCharacterKerning("_", .256f).401 /*_*/
//,new PerCharacterKerning("`", .256f).24  /*`*/
//,new PerCharacterKerning("a", .256f).401 /*a*/
//,new PerCharacterKerning("b", .256f).401 /*b*/
//,new PerCharacterKerning("c", .256f).36  /*c*/
//,new PerCharacterKerning("d", .256f).401 /*d*/
//,new PerCharacterKerning("e", .256f).401 /*e*/
//,new PerCharacterKerning("f", .256f).189 /*f*/
//,new PerCharacterKerning("g", .256f).401 /*g*/
//,new PerCharacterKerning("h", .256f).401 /*h*/
//,new PerCharacterKerning("i", .256f).16  /*i*/
//,new PerCharacterKerning("j", .256f).16  /*j*/
//,new PerCharacterKerning("k", .256f).36  /*k*/
//,new PerCharacterKerning("l", .256f).16  /*l*/
//,new PerCharacterKerning("m", .256f).6   /*m*/
//,new PerCharacterKerning("n", .256f).401 /*n*/
//,new PerCharacterKerning("o", .256f).401 /*o*/
//,new PerCharacterKerning("p", .256f).401 /*p*/
//,new PerCharacterKerning("q", .256f).401 /*q*/
//,new PerCharacterKerning("r", .256f).24  /*r*/
//,new PerCharacterKerning("s", .256f).36  /*s*/
//,new PerCharacterKerning("t", .256f).201 /*t*/
//,new PerCharacterKerning("u", .256f).401 /*u*/
//,new PerCharacterKerning("v", .256f).36  /*v*/
//,new PerCharacterKerning("w", .256f).52  /*w*/
//,new PerCharacterKerning("x", .256f).36  /*x*/
//,new PerCharacterKerning("y", .256f).36  /*y*/
//,new PerCharacterKerning("z", .256f).36  /*z*/
//,new PerCharacterKerning("{", .256f).241 /*{*/
//,new PerCharacterKerning("|", .256f).188 /*|*/
//,new PerCharacterKerning("}", .256f).241 /*}*/
//,new PerCharacterKerning("~", .256f).421 /*~*/
//};
//        float[] perCharKerning = new float[perCharKerningDouble.Length];

//        for (int x = 0; x<perCharKerning.Length; x++) {
//            perCharKerning[x] = (float) perCharKerningDouble[x];
//        }
//        return perCharKerning;
//    }
//}



[System.Serializable]
public class PerCharacterKerning {
    //these are named First and Second not because I'm terrible at naming things, but to make it look and act more like Custom Font natively do within Unity
    public string First = ""; //character
    public float Second; //kerning ex: 0.201

    public PerCharacterKerning(string character, float kerning) {
        this.First = character;
        this.Second = kerning;
    }

    public PerCharacterKerning(char character, float kerning) {
        this.First = "" + character;
        this.Second = kerning;
    }

    public char GetChar() {
        return First[0];
    }
    public float GetKerningValue() { return Second; }
}