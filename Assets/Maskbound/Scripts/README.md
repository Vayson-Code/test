# Maskbound Character Controller
### Professional Third-Person Controller for Parkour Combat with Mask Abilities

A complete, production-ready character controller system for Unity featuring:
- **Smooth third-person movement** with sprint and acceleration
- **Combat system** with combo chains and hit detection
- **Parkour mechanics** (wall run, slide, vault, ledge climb)
- **Mask ability system** with 3 mask types and 4 unique abilities
- **Professional animations** with blend trees and state machines
- **Responsive input** supporting keyboard/mouse and gamepad

---

## 📋 Features

### Core Movement
- Walk, Run, Sprint with smooth acceleration
- Variable jump height with coyote time & jump buffering
- Grounded detection with configurable ground check
- Camera-relative movement with smooth rotation
- Gravity system with proper physics

### Combat System
- 4-hit combo chain with reset timer
- Attack range detection with sphere overlap
- Damage calculation per combo stage
- Hit effects (hitstop, camera shake)
- Combat state management
- Heavy attack support

### Parkour System
- **Slide**: High-speed ground slide with cooldown
- **Wall Run**: Run along walls with automatic detection
- **Vault**: Jump over obstacles smoothly
- **Ledge Grab**: Climb ledges automatically

### Mask Ability System
**3 Mask Types:**
- **Wind**: Enhanced air control, extended dashes, glide effects
- **Stone**: Heavy impact, stuns enemies, breaks obstacles
- **Flame**: Speed bursts, burning trails, fire damage

**4 Abilities:**
- **Dash**: Fast directional dash with mask modifiers
- **Grapple**: Pull to grapple points or enemies
- **Slam**: Ground pound with impact effects
- **Blink**: Short-range teleport with phase capabilities

---

## 🚀 Quick Start

### Requirements
- Unity 2021.3 or newer
- Unity Input System package
- TextMeshPro package

### Installation

1. **Import the Scripts**
   - Copy all scripts from `MaskboundController/Scripts/` to your project
   - Organize them in your Assets folder maintaining the folder structure

2. **Setup Input System**
   - Import the `PlayerInputActions.inputactions` file
   - Window → Asset Management → Input Actions
   - Click "Generate C# Class" if needed

3. **Create the Player**
   ```
   1. Create empty GameObject named "Player"
   2. Add CharacterController component
   3. Add Animator component
   4. Add all scripts:
      - ThirdPersonController
      - CombatSystem
      - MaskManager
      - ParkourSystem
   5. Add Rigidbody (if needed for certain interactions)
   ```

4. **Configure CharacterController**
   - Height: 2
   - Radius: 0.5
   - Center: (0, 1, 0)

5. **Setup Ground Check**
   ```
   1. Create empty child object: "GroundCheck"
   2. Position at feet: (0, 0.1, 0)
   3. Assign to ThirdPersonController
   4. Set Ground Layers
   ```

6. **Setup Attack Point**
   ```
   1. Create empty child: "AttackPoint"
   2. Position in front: (0, 1, 0.75)
   3. Assign to CombatSystem
   ```

---

## ⚙️ Component Configuration

### ThirdPersonController Settings

```
Movement:
- Walk Speed: 3
- Run Speed: 6
- Sprint Speed: 8.5
- Rotation Speed: 15

Jump:
- Jump Height: 2
- Gravity: -24
- Coyote Time: 0.12s
- Jump Buffer: 0.12s

Ground Check:
- Radius: 0.2
- Layer: Default, Ground
```

### CombatSystem Settings

```
Combat:
- Combo Reset Time: 1.5s
- Attack Cooldown: 0.1s
- Max Combo: 4

Damage:
- Combo 1: 10
- Combo 2: 15
- Combo 3: 20
- Combo 4: 30

Attack:
- Range: 1.5
- Enemy Layers: Enemy

Effects:
- Hit Stop: 0.06s
- Camera Shake: 0.2
```

### MaskManager Settings

```
Masks:
- Available Masks: [Create MaskData assets]
- Starting Index: 0
- Ability Cooldown: 2.6s
- Show Switch UI: true
```

### ParkourSystem Settings

```
Slide:
- Speed: 10
- Duration: 1.5s
- Cooldown: 1s
- Controller Height: 0.5

Wall Run:
- Speed: 8
- Duration: 2s
- Wall Distance: 0.7
- Jump Up Force: 10
- Jump Side Force: 12

Vault:
- Height: 1.5
- Forward Force: 5
- Check Distance: 1

Ledge:
- Grab Height: 2
- Climb Speed: 3
```

---

## 🎮 Input Mapping

### Keyboard & Mouse
- **WASD**: Move
- **Space**: Jump
- **Left Shift**: Sprint
- **Left Mouse**: Attack
- **E**: Ability
- **Tab**: Switch Mask
- **Ctrl**: Slide (can be mapped)

### Gamepad
- **Left Stick**: Move
- **A/Cross**: Jump
- **LT**: Sprint
- **X/Square**: Attack
- **RT**: Ability
- **Y/Triangle**: Switch Mask

---

## 🎨 Creating Mask Data

1. **Create MaskData Asset**
   ```
   Right-click in Project → Create → Maskbound → Mask Data
   ```

2. **Configure the Mask**
   ```
   Identity:
   - Name: "Wind Mask"
   - Type: Wind
   - Icon: [Your sprite]
   - Description: "Grants enhanced mobility"

   Ability:
   - Type: Dash
   - Cooldown: 2.6

   Modifiers:
   - Speed: 1.2
   - Jump Height: 1.1
   - Air Control: 1.35

   Parameters:
   - Dash Distance: 9
   - Dash Duration: 0.18

   Effects:
   - Color: Cyan
   - Effect Prefab: [Optional particle]
   - Sound: [Optional audio clip]

   Wind Specific:
   - Upward Boost: 0.15
   ```

3. **Add to Manager**
   - Select Player GameObject
   - Add mask to MaskManager's "Available Masks" list

### Example Masks

**Wind Mask** (Dash)
- Enhanced mobility and air control
- Extended dash distance
- Upward boost on ability exit

**Stone Mask** (Slam)
- Heavy impact with stun
- Breaks fragile obstacles
- Ground pound creates shockwave

**Flame Mask** (Grapple/Blink)
- Speed bursts and explosions
- Burning trails
- Fire damage over time

---

## 🎬 Animation Setup

### Required Animator Parameters

**Locomotion:**
- `speed` (Float): Movement speed
- `IsGrounded` (Bool): Ground state
- `Jump` (Trigger): Jump action
- `FreeFall` (Bool): Falling state
- `MoveX` (Float): Relative X movement
- `MoveY` (Float): Relative Z movement
- `MotionSpeed` (Float): Animation speed multiplier

**Combat:**
- `Attack` (Trigger): Basic attack
- `ComboIndex` (Int): Current combo number (0-3)
- `HeavyAttack` (Trigger): Heavy attack
- `InCombat` (Bool): Combat stance

**Parkour:**
- `Slide` (Bool): Sliding state
- `WallRun` (Bool): Wall running
- `WallRunSide` (Float): -1 left, 1 right
- `Vault` (Trigger): Vault action
- `Climb` (Trigger): Ledge climb

**Masks:**
- `UseAbility` (Trigger): Ability activation
- `MaskType` (Int): 0=Wind, 1=Stone, 2=Flame

### Animation Events

**CombatSystem requires:**
- `OnAttackHit()` - Called at hit frame of attack animations

Add this event to your attack animation clips at the impact frame.

---

## 🎯 Usage Examples

### Basic Player Setup
```csharp
// In your game manager or level script
GameObject player = GameObject.FindGameObjectWithTag("Player");
ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
MaskManager maskManager = player.GetComponent<MaskManager>();

// Switch to specific mask
maskManager.EquipMaskByIndex(1); // Equip second mask

// Disable player control
controller.SetMovementEnabled(false);
```

### Adding Custom Abilities
```csharp
// Create custom ability behavior
public class CustomAbility : MonoBehaviour
{
    public void PerformAbility(MaskData mask)
    {
        // Your custom ability logic
    }
}

// Modify MaskData.cs ExecuteAbility() to call your custom abilities
```

### Handling Combat Events
```csharp
CombatSystem combat = GetComponent<CombatSystem>();

combat.OnComboIncreased += (combo) => {
    Debug.Log($"Combo: {combo}");
};

combat.OnHitEnemy += (enemy, damage) => {
    Debug.Log($"Hit {enemy.name} for {damage} damage");
};

combat.OnComboReset += () => {
    Debug.Log("Combo reset!");
};
```

### Custom Mask Modifiers
```csharp
// Extend MaskData for custom behaviors
[CreateAssetMenu(fileName = "Custom Mask", menuName = "Maskbound/Custom Mask")]
public class CustomMaskData : MaskData
{
    public float customParameter;
    
    public override void ExecuteAbility(GameObject player, ThirdPersonController controller)
    {
        // Custom ability implementation
        base.ExecuteAbility(player, controller);
        // Add your modifications
    }
}
```

---

## 🐛 Troubleshooting

### Player Not Moving
- Check Input System package is installed
- Verify PlayerInput component is added
- Check Actions are mapped correctly
- Ensure "Actions" is selected in PlayerInput inspector

### Abilities Not Working
- Verify MaskData assets are created
- Check cooldown hasn't just been used
- Ensure MaskManager has masks assigned
- Check ability components are present (DashAbility, etc.)

### Combat Not Detecting Hits
- Verify Attack Point is positioned correctly
- Check Enemy layer mask in CombatSystem
- Ensure enemies have colliders
- Implement IDamageable interface on enemies

### Parkour Not Triggering
- Check wall layers in ParkourSystem
- Verify wall check distances
- Ensure obstacles have colliders
- Check if player has minimum speed

### Animation Not Playing
- Verify all animator parameters exist
- Check animator controller is assigned
- Ensure animation clips are in controller
- Check parameter names match exactly (case-sensitive)

---

## 🔧 Advanced Customization

### Adding New Mask Types

1. Add to `MaskType` enum in `MaskData.cs`
2. Update combo matrix in ability implementations
3. Create mask-specific visual effects
4. Add new modifiers to MaskData properties

### Custom Parkour Actions

1. Add methods to `ParkourSystem.cs`
2. Create detection logic in `CheckForParkourOpportunities()`
3. Implement action coroutine
4. Add animation parameters

### Extended Combat System

```csharp
// Add to CombatSystem.cs
public void PerformSpecialAttack()
{
    // Special attack logic
}

public void PerformAerialCombo()
{
    if (!characterController.isGrounded)
    {
        // Aerial combat logic
    }
}
```

---

## 📝 Code Architecture

```
Core/
├── ThirdPersonController.cs    # Main movement controller
├── CombatSystem.cs             # Combat and combo system
└── Interfaces                  # IDamageable, IBreakable, IStunnable

Movement/
└── ParkourSystem.cs            # Wall run, slide, vault, climb

Masks/
├── MaskManager.cs              # Mask switching and management
├── MaskData.cs                 # ScriptableObject for masks
├── DashAbility.cs              # Dash implementation
└── AbilityImplementations.cs   # Grapple, Slam, Blink

UI/
└── MaskboundUI.cs              # HUD and UI management

Utilities/
└── (Add helper scripts here)
```

---

## 🎓 Best Practices

1. **Layer Setup**: Create layers for "Ground", "Enemy", "Wall", "GrapplePoint"
2. **Tag Setup**: Tag grapple points with "GrapplePoint"
3. **Physics**: Use continuous collision detection for fast-moving player
4. **Optimization**: Pool particle effects and temporary objects
5. **Testing**: Test each ability independently before combining
6. **Animation**: Use animation root motion for cinematic moves
7. **Balance**: Tune values through playtesting, start with provided defaults

---

## 📦 Package Contents

- ✅ Complete movement system
- ✅ Combat with combo system
- ✅ 4 parkour mechanics
- ✅ 3 mask types with modifiers
- ✅ 4 unique abilities
- ✅ Input System configuration
- ✅ UI components
- ✅ Extensible architecture
- ✅ Well-commented code
- ✅ Example configurations

---

## 🤝 Support & Credits

**Created for the Maskbound 48h Game Jam Project**

Based on GDD specifications with enhanced features:
- Robust third-person controller
- Professional combat system
- Advanced parkour mechanics
- Flexible mask system
- Production-ready code

### Recommended Additions
- Cinemachine for camera system
- Post-processing for visual effects
- Audio mixer for sound management
- Particle effects library

---

## 📄 License

This character controller system is provided as-is for use in the Maskbound project.
Feel free to modify and extend for your specific needs.

---

## 🚀 Getting Started Checklist

- [ ] Import all scripts
- [ ] Setup Input System
- [ ] Create Player GameObject
- [ ] Add required components
- [ ] Configure CharacterController
- [ ] Setup Ground Check
- [ ] Setup Attack Point
- [ ] Create MaskData assets
- [ ] Configure layers and tags
- [ ] Setup Animator
- [ ] Add animation events
- [ ] Create UI canvas
- [ ] Test basic movement
- [ ] Test combat
- [ ] Test parkour
- [ ] Test mask abilities
- [ ] Polish and tune values

**Ready to create an amazing parkour combat experience!**
