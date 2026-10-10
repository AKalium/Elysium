# Reusable dialogue prefab

`Dialogue.prefab` contains the canvas, green gradient, character portrait, brown
dialogue panel, nameplate, progress text, Continue button and dialogue controller.
`IntroScene` uses a linked instance of this prefab. Its layout is visible and
editable before entering Play mode.

To reuse it:

1. Drag `Assets/Intro/Dialogue.prefab` into a scene.
2. Select the prefab root and edit **Mascot Name**, **Idle Texture**,
   **Talking Texture**, **Dialogue Lines**, and **Characters Per Second**.
3. Set **Next Scene** to an enabled scene in the build scene list. Leave it blank
   to close the dialogue in the current scene. Re-enabling it restarts the dialogue.

Use `{name}` in a line to insert the mascot name and `<i>...</i>` for italic text.
Continue reveals unfinished text, then advances once the text is fully visible.

Double-click the prefab to change its layout and colors in Prefab Mode. Edit an
instance for a scene-specific override, or make a Prefab Variant for another style.
The UI references and Continue click event are already wired up. Keep those
references assigned when changing the hierarchy.

The camera and EventSystem are scene objects, outside the prefab. The controller
uses an existing EventSystem or creates one if needed. The gradient and rounded
panel assets are shared; they are not generated or destroyed at runtime.
