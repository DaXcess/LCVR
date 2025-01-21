# Item Offset Editor

By launching Lethal Company with the `--lcvr-item-offset-editor` flag, you enable the Item Offset Editor.

The offset editor is nothing more than an invisible object added to the scene, from which it's position and rotation is used as an offset for held items, overriding built-in offsets or offsets provided by an `*.lcvr-cfg.json` file.

> **[UnityExplorer](https://thunderstore.io/c/lethal-company/p/LethalCompanyModding/Yukieji_UnityExplorer/) or any similar mod is required for you to be able to use the Item Offset Editor. You must be in VR to be able to use the Item Offset Editor. Use a mod like [DevelopmentStartup](https://thunderstore.io/c/lethal-company/p/CTNOriginals/DevelopmentStartup/) to bypass the main menu, since Unity Explorer may break UI interactions.**

> You would most likely want to use the LCVR development build, since you can use the debug menu to spawn in the items you want. You may also use other mods to spawn items.

Once you are in a lobby, open up Unity Explorer, and look for an object called `VR Item Offset Editor` (it should be in the root of the scene). Modifying the position or rotation of this object will alter how items are held in your hand.

Once you have found a desired position and rotation offset for your item, make note of the X, Y and Z coordinates of both the position and rotation. You may now use these values in the `itemOffsets` configuration, which is documented [here](README.md#item-offsets).