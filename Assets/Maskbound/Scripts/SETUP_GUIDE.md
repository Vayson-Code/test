# Maskbound Controller - Quick Setup Guide

## Step-by-Step Setup (15 minutes)

### 1. Prerequisites (2 min)
```
✓ Unity 2021.3+
✓ Input System package installed (Window → Package Manager → Input System)
✓ TextMeshPro imported (will prompt on first use)
```

### 2. Import Scripts (3 min)

Create this folder structure in your Assets:
```
Assets/
└── Maskbound/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── ThirdPersonController.cs
    │   │   └── CombatSystem.cs
    │   ├── Movement/
    │   │   └── ParkourSystem.cs
    │   ├── Masks/
    │   │   ├── MaskManager.cs
    │   │   ├── MaskData.cs
    │   │   ├── DashAbility.cs
    │   │   └── AbilityImplementations.cs
    │   └── UI/
    │       └── MaskboundUI.cs
    └── Input/
        └── PlayerInputActions.inputactions
```

### 3. Setup Input System (2 min)

1. Import `PlayerInputActions.inputactions`
2. Select the file in Project window
3. Click "Generate C# Class" in Inspector
4. If popup appears: "The Input System Package requires a restart"
   - Click "Yes" to restart Unity

### 4. Create Player GameObject (5 min)

**Hierarchy Setup:**
```
Player (Empty GameObject)
├── Model (Your character mesh/armature)
├── GroundCheck (Empty, Position: 0, 0.1, 0)
├── AttackPoint (Empty, Position: 0, 1, 0.75)
└── Main Camera (or use Cinemachine)
```

**Add Components to Player:**
1. CharacterController
   - Height: 2
   - Radius: 0.5
   - Center: (0, 1, 0)
   
2. Animator
   - Controller: (Your animator controller)
   - Apply Root Motion: ✓ (optional, for cinematic moves)
   
3. Player Input (Component)
   - Actions: PlayerInputActions
   - Default Map: Player
   - Behavior: Invoke Unity Events
   
4. ThirdPersonController
   - Move Speed: 6
   - Sprint Speed: 8.5
   - Jump Height: 2
   - Ground Check: (Drag GroundCheck object)
   - Ground Layers: Default, Ground
   - Camera Transform: (Drag Main Camera)
   
5. CombatSystem
   - Attack Point: (Drag AttackPoint object)
   - Attack Range: 1.5
   - Enemy Layers: Enemy
   - Combo Damage: 10, 15, 20, 30
   
6. ParkourSystem
   - Wall Layers: Wall
   - Slide Speed: 10
   - Wall Run Speed: 8
   
7. MaskManager
   - Available Masks: (Will add after creating masks)
   - Ability Cooldown: 2.6
   
8. Rigidbody (Optional, for physics interactions)
   - Use Gravity: ✓
   - Is Kinematic: ✓
   - Constraints: Freeze Rotation (X, Y, Z)

### 5. Create Mask Data Assets (3 min)

**Create 3 Mask Assets:**

**Wind Mask:**
```
Right-click → Create → Maskbound → Mask Data
Name: "WindMask"

Settings:
- Mask Name: "Wind"
- Mask Type: Wind
- Ability Type: Dash
- Speed Multiplier: 1.2
- Air Control Multiplier: 1.35
- Dash Distance: 9
- Dash Duration: 0.18
- Mask Color: Cyan (0, 1, 1, 1)
- Wind Upward Boost: 0.15
```

**Stone Mask:**
```
Right-click → Create → Maskbound → Mask Data
Name: "StoneMask"

Settings:
- Mask Name: "Stone"
- Mask Type: Stone
- Ability Type: Slam
- Speed Multiplier: 0.9
- Slam Force: 25
- Slam Radius: 2
- Mask Color: Gray (0.5, 0.5, 0.5, 1)
- Stone Mass Increase: 0.25
- Stone Stun Duration: 0.5
```

**Flame Mask:**
```
Right-click → Create → Maskbound → Mask Data
Name: "FlameMask"

Settings:
- Mask Name: "Flame"
- Mask Type: Flame
- Ability Type: Blink
- Speed Multiplier: 1.15
- Blink Distance: 6
- Mask Color: Orange (1, 0.5, 0, 1)
- Flame Speed Boost: 1.2
- Flame Burn DPS: 6
- Flame Burn Duration: 1.5
```

**Add Masks to Manager:**
- Select Player
- In MaskManager component
- Add all 3 masks to "Available Masks" list
- Set Starting Mask Index: 0

---

## Layer & Tag Setup

### Create Layers
1. Edit → Project Settings → Tags and Layers

Add these layers:
```
Layer 8: Ground
Layer 9: Enemy
Layer 10: Wall
Layer 11: GrapplePoint
```

### Create Tags
Add these tags:
```
- GrapplePoint
- Enemy
```

### Assign Layers
```
- Floor objects → Ground layer
- Wall objects → Wall layer
- Enemy objects → Enemy layer
- Grapple points → GrapplePoint layer + GrapplePoint tag
```

---

## Animator Setup

### Required Parameters (Float)
- speed
- MotionSpeed
- MoveX
- MoveY
- WallRunSide

### Required Parameters (Bool)
- IsGrounded
- FreeFall
- InCombat
- Slide
- WallRun

### Required Parameters (Int)
- ComboIndex
- MaskType

### Required Parameters (Trigger)
- Jump
- Attack
- HeavyAttack
- UseAbility
- Vault
- Climb

### Animation States Structure
```
Base Layer:
├── Idle
├── Locomotion Blend Tree (speed → Walk/Run animations)
│   └── Uses MoveX and MoveY for directional movement
├── Jump
├── Fall
└── Land

Grounded Sub-Layer:
├── Idle/Walk/Run
└── Slide

Combat Layer:
├── Combat Idle
├── Attack 1
├── Attack 2
├── Attack 3
├── Attack 4 (Finisher)
└── Heavy Attack

Upper Body Layer:
└── Ability animations

Parkour Layer:
├── Wall Run
├── Vault
└── Climb
```

### Animation Events
Add to attack animations:
- Event: `OnAttackHit`
- Time: At impact frame (usually 30-50% through animation)
- Function: OnAttackHit()
- Object: Keep empty (will call on GameObject with CombatSystem)

---

## Testing Checklist

### Basic Movement (Test in Play Mode)
- [ ] WASD movement works
- [ ] Character rotates toward movement direction
- [ ] Sprint (Shift) increases speed
- [ ] Jump (Space) works with variable height
- [ ] Coyote time allows jump shortly after ledge
- [ ] Jump buffering lets you press jump before landing

### Combat
- [ ] Left mouse click attacks
- [ ] Combo chains up to 4 hits
- [ ] Combo resets after 1.5s
- [ ] Hit detection works on enemies
- [ ] Combo counter displays on UI

### Parkour
- [ ] Slide activates when moving (add slide input)
- [ ] Wall run triggers on walls
- [ ] Vault works on low obstacles
- [ ] Ledge grab/climb works

### Masks & Abilities
- [ ] Tab switches between masks
- [ ] E triggers current mask ability
- [ ] Cooldown displays correctly
- [ ] Each mask has unique behavior:
  - Wind: Extended dash with air control
  - Stone: Heavy slam with stun
  - Flame: Blink with explosive arrival

---

## Common Issues & Fixes

### "Player not moving"
- ✓ Check Input System is installed
- ✓ Verify Player Input component has Actions assigned
- ✓ Ensure "Player" action map is selected
- ✓ Check console for input errors

### "Abilities not working"
- ✓ Add mask data to Available Masks list
- ✓ Wait for cooldown after first use
- ✓ Check ability key is mapped (E by default)

### "Combat not detecting hits"
- ✓ Position Attack Point in front of player
- ✓ Add colliders to enemies
- ✓ Set Enemy layer on enemy objects
- ✓ Add IDamageable script to enemies

### "Animations not playing"
- ✓ Verify animator parameters match exactly
- ✓ Check animator controller is assigned
- ✓ Ensure transitions are set up
- ✓ Add animation event to attack clips

### "Ground check not working"
- ✓ Create GroundCheck child object at feet
- ✓ Assign to ThirdPersonController
- ✓ Set Ground Layers (include Ground layer)
- ✓ Adjust radius if needed (0.2 default)

---

## Performance Tips

1. **Object Pooling**: Pool particle effects and damage numbers
2. **Layer-based Collision**: Use layer collision matrix to reduce checks
3. **LOD**: Use LOD groups for character model if complex
4. **Occlusion Culling**: Enable for large levels
5. **Animator Culling**: Set to "Cull Update Transforms"

---

## Next Steps

After basic setup:

1. **Polish Camera**
   - Install Cinemachine
   - Create FreeLook camera
   - Add camera shake on hits

2. **Add VFX**
   - Particle effects for abilities
   - Trail effects for dashes
   - Impact effects for slams
   - Burn effect for flame mask

3. **Improve Audio**
   - Footstep sounds (via animation events)
   - Combat impact sounds
   - Ability sound effects
   - Background music

4. **Enhanced UI**
   - Health bar
   - Stamina/energy system
   - Damage numbers
   - Screen effects (hit flash, low health vignette)

5. **Enemy AI**
   - Implement IDamageable interface
   - Add basic AI behavior
   - Health system
   - Death animations

---

## Quick Reference: Default Controls

**Keyboard & Mouse:**
```
W, A, S, D     - Move
Space          - Jump
Left Shift     - Sprint
Left Mouse     - Attack
E              - Ability
Tab            - Switch Mask
Ctrl (custom)  - Slide
```

**Gamepad:**
```
Left Stick     - Move
A/Cross        - Jump
LT/L2          - Sprint
X/Square       - Attack
RT/R2          - Ability
Y/Triangle     - Switch Mask
```

---

## Configuration Values Reference

```csharp
// ThirdPersonController
walkSpeed = 3f;
runSpeed = 6f;
sprintSpeed = 8.5f;
jumpHeight = 2f;
gravity = -24f;
coyoteTime = 0.12f;
jumpBufferTime = 0.12f;

// CombatSystem
comboResetTime = 1.5f;
attackCooldown = 0.1f;
comboDamage = [10, 15, 20, 30];
attackRange = 1.5f;

// MaskManager
abilityCooldown = 2.6f;

// ParkourSystem
slideSpeed = 10f;
slideDuration = 1.5f;
wallRunSpeed = 8f;
wallRunDuration = 2f;
vaultHeight = 1.5f;
```

---

**You're all set! Start playing and have fun!** 🎮
