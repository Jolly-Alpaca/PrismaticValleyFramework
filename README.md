## Prismatic Valley Framework
Framework for adding the prismatic effect to other objects in Stardew Valley

## Getting Started

### Examples

"CustomFields": {
  "JollyLlama.PrismaticValleyFramework/Color": "Custom Palette",
  "JollyLlama.PrismaticValleyFramework/Palette": "#01084F,#57234A,#BC355D,#CD5348,#F6BB5D,#7C6256"
}

## A Lesson on How MonoGame Applies Color
### Introduction
This framework works by patching the color that is passed to MonoGame's draw method for a given texture. The primary purpose of this color is to add a tint to the texture being draw to the screen. However, how that color is applied may be contrary to how one may expect it to be applied. 

As an example, take this red egg texture:

![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6)

If you apply a blue tint to it, you may expect the color to be purple:

![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6) + 
![BlueTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/b67e1a4e-810d-4332-92ac-09f9379744fe) =
![PurpleEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/a03e3f74-3021-4d81-b326-bd884504e930)

Or if you apply a white tint, you may expect the color to become lighter:

![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6) + 
![WhiteTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/8dd40bff-d57c-4635-b2e5-eb45efb4ba34) =
![LightRedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/ffbf678e-8e42-472f-8838-155fe36fa994)

What if you take this purple egg and apply a matching purple tint to it? You may expect the color to remain unchanged:

![PurpleEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/85473352-0905-45d8-a779-30b665515dc8) +
![PurpleTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/f399dec9-0fcf-4d2b-97e1-a24b625c0ca1) =
![PurpleEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/85473352-0905-45d8-a779-30b665515dc8)

However, this is not how MonoGame handles tinting. Instead, you get the following:

![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6) + 
![BlueTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/b67e1a4e-810d-4332-92ac-09f9379744fe) =
![BlackEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/07f7fbcd-beb8-4ae9-8f34-175356be7723)<sup>*</sup>

![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6) + 
![WhiteTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/8dd40bff-d57c-4635-b2e5-eb45efb4ba34) =
![RedEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/c9cd2335-854b-4a0d-abe4-ed4699134cf6)

![PurpleEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/85473352-0905-45d8-a779-30b665515dc8) +
![PurpleTint](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/f399dec9-0fcf-4d2b-97e1-a24b625c0ca1) =
![DarkPurpleEgg](https://github.com/Jolly-Alpaca/PrismaticValleyFramework/assets/140008804/501a0dfa-130d-4c87-a381-990303268e7c)


<sub><sup><sup>*</sup>The actual texture is entirely black. The gray outline is left for dark mode users.</sup></sub>

### How it works
TLDR: If you'd rather skip the details on what MonoGame is doing or already know all this, the important takeaway is the closer your texture colors are to RGB (255 255 255), the closer to the tint color your texture in game will be. Stick to whites and light grays to maximize the saturation levels when your texture is drawn. 

MonoGame uses [multiply blend mode](https://en.wikipedia.org/wiki/Blend_modes#Multiply) when applying the color to the texture. Multiply blending takes the RGB values of the texture and multiplies them with the RGB values of the tint color. However, this is done using the 0-1 (float) scale, not the 0-255 scale you may be more familiar with.

Examples
- Red: (255 0 0) => (1 0 0)
- Blue: (0 0 255) => (0 0 1)
- White: (255 255 255) => (1 1 1)
- Purple: (128 0 128) ~> (0.5 0 0.5)

As a result, the resulting color will never be lighter than the original texture; only darker or the same. 

Let's take the above red tinted blue example above. Why is it black?
```
R: (Red) 1 * (Blue) 0 = 0
G: (Red) 0 * (Blue) 0 = 0
B: (Red) 0 * (Blue) 1 = 0
Result: (0 0 0) => Black
```

Red tinted white:
```
R: (Red) 1 * (White) 1 = 1
G: (Red) 0 * (White) 1 = 0
B: (Red) 0 * (White) 1 = 0
Result: (1 0 0) => Red
```

Purple tinted purple:
```
R: (Purple) 0.5 * (Purple) 0.5 = 0.25
G: (Purple) 0 * (Purple) 0 = 0
B: (Purple) 0.5 * (Purple) 0.5 = 0.25
Result: (0.25 0 0.25) => Darker Purple
```

### Conclusion
In the majority of cases, the desired outcome is to draw the texture as is, with no tint applied. For this reason, White is the default tint color passed to the draw method as any number multiplied by 1 is itself. It is for this same reason, you will want your texture to comprise of mostly white and light grays (i.e. colors close to (1 1 1) i.e. (255 255 255)). This will minimize the changes made to the color you want to apply and prevent your texture from becoming too dark.

Fun fact: The darkening aspect of the Multiply blending is how the object silouettes are drawn in Stardew Valley. Since anything mulitplied by 0 is 0, any color multiplied by Black (0 0 0) is Black (0 0 0).
