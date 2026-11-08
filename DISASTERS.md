# Natural disasters

I added fires and small floods to SampleScene. The game pauses and fades to black,
then Dawny tells you what happened and how many buildings were damaged. Press
Continue to go back to the city. The fade takes 2 seconds each way, and the game
returns to whatever speed you were using before.

For now, Dawny only talks about the disaster since the pollution cutscene is
being worked on separately.

## How to test it

- Open SampleScene, press Play, and place some buildings.
- Press **K for a fire** or **L for a flood**. You don't need to raise pollution
  first; these keys skip the pollution requirements and cooldown for testing.
- Make sure the game is unpaused and any daily report or dialogue is closed.
- If the text is still appearing, the first Continue finishes the text and the
  next one closes the dialogue.
- There needs to be at least one building that can be damaged. Roads and the
  Mayor's Office don't count.

## When disasters happen

The game checks once per day, after you close the daily report:

- Above 25 pollution: 10% chance of a flood.
- Above 50 pollution: 10% chance of a fire, with floods still possible.
- If both get picked, the game randomly chooses one. You won't get both on the
  same day.
- After a disaster, the next 3 days are skipped. If one happens on day 5, the
  next possible day is day 9. Triggering one with a key also starts this
  cooldown, but you can keep using the keys to test.

## What happens to buildings

Fire damages 1/6 of the available buildings, rounded up. Flood damages 2
buildings or 1/12 of the available buildings, whichever is larger, also rounded
up. If there aren't enough buildings, it only damages what's available.

Buildings are picked randomly. Roads, the Mayor's Office, and buildings that
are already damaged are left out.

Fire-damaged buildings turn **red** and flooded buildings turn **blue**. These
are temporary visuals until we have destroyed building sprites.

Damaged buildings stay on the map, but stop providing housing, jobs, services,
income, and pollution removal. They also stop producing pollution and charging
upkeep. Losing housing lowers population, and losing jobs or services can
affect happiness. You can still sell them for the normal refund and rebuild
in that space.

## Dawny and the UI

Dawny's appearance depends on pollution:

- 0–25: level 1
- 26–50: level 2
- Above 50: level 3

I'm using the top pose from the level 3 sprite sheet. The original images
haven't been changed.

The setup is in `Assets/Disasters/NaturalDisasters.prefab`, which is already
in SampleScene. It uses our existing `Assets/Intro/Dialogue.prefab`, so the
dialogue UI can still be edited there. The intro still works the same way.
You can change the fade duration, disaster chance, cooldown, and portrait
framing on the disaster prefab. Turn off **Enable Test Keys** when we don't
want the shortcuts available anymore.

For whoever is working on the pollution cutscene: check
`DisasterSystem.IsCutsceneOpen` before showing it so the two don't overlap.
The disaster system already waits for active `IntroDialogue` conversations
and for the clock to be unpaused.

## Notes for working on this later

`DisasterDamage` keeps track of whether a building is damaged and what happened
to it. We can replace the tint with destroyed sprites there later.
`GridPlaceable.IsOperational` is what the other systems check before counting
a building's contributions. The building itself stays enabled so selling and
occupied grid spaces still work.

There are checks under **Elysium > Verify Disaster Rules** and
**Elysium > Verify Disasters in Play Mode**. Save SampleScene before running
the Play Mode check. It creates a temporary test city, checks the disasters,
and exits Play Mode without saving those buildings. Results go in `Logs`.
Interactive runs also request screenshots; batch runs don't capture the Game view.

Damage currently only lasts for the current game session. When we add saving
and loading, we'll need to save each building's damage too.
