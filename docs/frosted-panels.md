# Frosted UI backgrounds

Use the existing image prefab with `FrostedImage` as its behavior:

```csharp
new VirtualProp(PropType.Image, 0.7f, down, typeof(FrostedImage),
    new VirtualProp(PropType.Text, 1f))
```

`FrostedImage` derives from `BaseImage`, so configure its `image.color`, `image.sprite`, and `image.raycastTarget` as usual. Color alpha controls tint strength (for example 0.8 for a dark translucent panel); CanvasGroup alpha controls visibility. A zero color alpha hides the background. Sprite alpha and Unity UI masks clip the blur. Text and other child controls render sharply above it.

The main camera's `MenuBackgroundBlur` captures a shared scene texture only while a frosted image is active. It uses the existing screen-scaled radius. Panels sample the screen region behind them instead of stretching the entire scene into each rectangle. Outside the tutorial card/hint panel the camera remains sharp; the welcome overlay still dims the surroundings. The main menu retains its full-screen blur.

The scene is progressively downsampled with a 2x2 averaging filter before the horizontal and vertical Gaussian blur. This keeps blur samples close together to avoid repeated edges and square patterns. The shared result is bilinearly upsampled for display.

This implementation targets the project's built-in render pipeline and screen-space overlay canvas on the main camera. It blurs the 3D scene, not other UI layered underneath a panel. Intermediate textures are resized with the camera output and released when no longer needed.
