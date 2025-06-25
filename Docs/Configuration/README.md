# Mod Configuration

Not to be confused with the BepInEx config file (`lcvr.daxcess.io.cfg`), the LCVR Mod Config file is a JSON file mod authors can ship with their mod package to influence how LCVR behaves with parts of their mods.

During startup, LCVR enumerates the `BepInEx/Plugins` directory and looks for files called `*.lcvr-cfg.json`, where the wildcard can be anything, as long as the file ends with `.lcvr-cfg.json`.

The configuration file is structured in a simple key-value pair style, where the key annotates what is being configured, and the value contains the values for that specific configuration.

## Configurations

### Version

| key       | required |
|-----------|----------|
| `version` | Yes      |

Every configuration file is required to have a version annotation. This version field is used by LCVR to determine which configuration schema to use, and whether it is still compatible.

The version is an integer, starting at `1`, and increasing any time LCVR receives an update that changes the configuration schema.

The current configuration schema version can be found in LCVR's top level README.md file.

### Item Offsets

| key           | required |
|---------------|----------|
| `itemOffsets` | No       |

The Item Offsets configuration can be used to give modded items a custom holding offset. Since holding items in VR works a bit differently, many modded items are held in strange positions and/or rotations.

You can use the [Item Offset Editor](ITEM_OFFSET_EDITOR.md) to find correct offsets for your desired item.

An item offset is structured as follows:

```json
{
  "itemOffsets": {
    "ItemName (CaSe SeNSiTiVe!)": {
      "position": {
        "x": 0,
        "y": 0,
        "z": 0
      },
      "rotation": {
        "x": 0,
        "y": 0,
        "z": 0
      }
    },
    "AnotherItem": {
      ...
    },
    "AThirdItem": {
      ...
    }
  }
}
```

> Note that item names are case-sensitive

Both the `position` and `rotation` keys are **required** when creating an item offset. They are both represented by a `Vector3`, and allow both integers and floating point numbers.

### Shovels

| key       | required |
|-----------|----------|
| `shovels` | No       |

Any mod that adds a custom item that behaves the same as a vanilla Shovel (inherits from the `Shovel` class) can be added in the Shovels configuration, so that VR players can swing these items just like vanilla shovels/signs.

Custom shovels are annotated by an array of item names.

**Example usage**:

```json
{
  "shovels": [
    "MyCustomShovel",
    "Broom",
    "AVeryLargeStick",
    ...
  ]
}
```

> Note that item names are case-sensitive

## Example config file

> This configuration file is equivalent to LCVR's built-in configuration

```json
{
  "version": 1,
  "itemOffsets": {
    "ChemicalJug": {
      "position": {
        "x": -0.05,
        "y": 0.14,
        "z": -0.29
      },
      "rotation": {
        "x": 0,
        "y": 90,
        "z": 120
      }
    },
    "ToiletPaperRolls": {
      "position": {
        "x": 0,
        "y": 0.13,
        "z": -0.4
      },
      "rotation": {
        "x": 0,
        "y": 90,
        "z": 90
      }
    },
    "Boombox": {
      "position": {
        "x": -0.02,
        "y": 0.1,
        "z": -0.29
      },
      "rotation": {
        "x": 90,
        "y": 285,
        "z": 0
      }
    },
    "LungApparatus": {
      "position": {
        "x": -0.04,
        "y": 0.12,
        "z": -0.21
      },
      "rotation": {
        "x": 0,
        "y": 270,
        "z": 0
      }
    },
    "Cog1": {
      "position": {
        "x": -0.04,
        "y": 0.24,
        "z": -0.33
      },
      "rotation": {
        "x": 0,
        "y": 270,
        "z": 100
      }
    },
    "CashRegister": {
      "position": {
        "x": -0.09,
        "y": 0.13,
        "z": -0.46
      },
      "rotation": {
        "x": 0,
        "y": 75,
        "z": 255
      }
    },
    "EnginePart1": {
      "position": {
        "x": -0.04,
        "y": 0.33,
        "z": -0.3
      },
      "rotation": {
        "x": 0,
        "y": 270,
        "z": 90
      }
    },
    "ExtensionLadder": {
      "position": {
        "x": -0.2,
        "y": 0.28,
        "z": -0.47
      },
      "rotation": {
        "x": 90,
        "y": 90,
        "z": 0
      }
    },
    "FancyPainting": {
      "position": {
        "x": 0.05,
        "y": 0.75,
        "z": -0.06
      },
      "rotation": {
        "x": 6,
        "y": 270,
        "z": 184
      }
    },
    "SoccerBall": {
      "position": {
        "x": -0.07,
        "y": 0.17,
        "z": -0.19
      },
      "rotation": {
        "x": 0,
        "y": 0,
        "z": 0
      }
    },
    "ControlPad": {
      "position": {
        "x": 0.06,
        "y": 0.09,
        "z": -0.23
      },
      "rotation": {
        "x": 90,
        "y": 90,
        "z": 0
      }
    },
    "GarbageLid": {
      "position": {
        "x": -0.02,
        "y": 0.11,
        "z": -0.08
      },
      "rotation": {
        "x": 0,
        "y": 0,
        "z": 90
      }
    },
    "RedLocustHive": {
      "position": {
        "x": 0.04,
        "y": 0.32,
        "z": -0.38
      },
      "rotation": {
        "x": 0,
        "y": 0,
        "z": 0
      }
    },
    "FishTestProp": {
      "position": {
        "x": 0,
        "y": 0.12,
        "z": -0.06
      },
      "rotation": {
        "x": 0,
        "y": 80,
        "z": 165
      }
    },
    "BeltBag": {
      "position": {
        "x": 0.02,
        "y": 0.09,
        "z": -0.18
      },
      "rotation": {
        "x": 0,
        "y": 90,
        "z": 0
      }
    },
    "CaveDwellerBaby": {
      "position": {
        "x": -0.07,
        "y": 0.02,
        "z": -0.11
      },
      "rotation": {
        "x": 6,
        "y": 218,
        "z": 85
      }
    },
    "Zeddog": {
      "position": {
        "x": -0.14,
        "y": 0.1,
        "z": -0.22
      },
      "rotation": {
        "x": 0,
        "y": 315,
        "z": 270
      }
    }
  },
  "shovels": [
    "Shovel",
    "YieldSign",
    "StopSign"
  ]
}
```