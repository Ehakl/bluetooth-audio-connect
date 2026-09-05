# iPhone shortcut: use your Bluetooth headphones

This guide creates a native Apple Shortcut that selects headphones as the **iPhone's audio playback destination**. It does not run the Windows app, control the PC, or install a Windows executable on iOS.

## What to install

- Apple's **Shortcuts** app on the iPhone. It is normally included; if removed, reinstall **Shortcuts by Apple** from the App Store.
- No software from this repository, server, SSH access, configuration profile or paid utility is needed.
- Pair your headphones with the iPhone in **Settings > Bluetooth** first, following the headphones' pairing instructions.

Keep Bluetooth enabled and the headphones awake and nearby. Connect them manually once before creating the shortcut so they appear as a selectable audio destination.

## Create the shortcut

1. Open **Shortcuts** on the iPhone.
2. Tap **+** to create a shortcut and name it **Connect Headphones**.
3. Tap **Add Action** (or use the action search field).
4. Search for **Playback Destination** and add **Set Playback Destination**. Some versions label the action **Change Playback Destination**; choose the action that sets the audio route.
5. If the action offers Set/Add/Remove, select **Set**.
6. Tap the destination in the action, usually **iPhone**, and select your headphones by name.
7. Save the shortcut. Tap its play button to test it while the headphones are awake.

The shortcut should contain just this action:

```text
Set playback destination to [your headphones]
```

It does not need a Bluetooth off/on action. Leaving the radio on avoids interrupting other Bluetooth accessories.

## Put it on the Home Screen

1. Open the shortcut's editor using its **...** button.
2. Open the shortcut's **Details** or name menu, depending on your iOS version.
3. Select **Add to Home Screen**.
4. Choose the name/icon and tap **Add**.

Tap that icon whenever you want the iPhone to use those headphones. You can also try saying **Siri, Connect Headphones** using the shortcut's name.

## Test the result

Play a short audio clip and check that sound comes from the headphones. Open Control Center and inspect the audio output picker to confirm the destination. Running a shortcut without an error is not, by itself, proof that the headphones connected.

## Limits and troubleshooting

- This selects a playback route; it is not a general command to force-connect any Bluetooth device. Availability depends on the headphones and iOS.
- If the headphones are missing from the destination list, connect them manually in **Settings > Bluetooth**, return to the shortcut editor, and select them again.
- If they are connected to the PC and cannot switch, disconnect **only those headphones** from the PC and try the iPhone shortcut again. You do not need to turn off the PC's Bluetooth radio or disconnect its mouse.
- AirPods may switch among compatible Apple devices automatically. Switching from Windows is not guaranteed by this shortcut.
- A sleeping device, depleted battery or out-of-range device cannot be fixed by selecting its audio route.
- This does not set a separate global microphone default like the Windows utility. iOS and the active calling/recording app determine the input route.
- A shared shortcut may need its destination reselected on the receiving iPhone. No signed/importable `.shortcut` file or iCloud sharing link is included; these are manual setup instructions.

These instructions are based on Apple documentation and have not been tested on your iPhone from this Windows workspace. Labels may differ by iOS version.

## Apple references

- [Apple: Set Playback Destination routes audio to AirPods or Bluetooth speakers](https://developer.apple.com/videos/play/wwdc2021/10283/)
- [Apple: Add a shortcut to the Home Screen](https://support.apple.com/guide/shortcuts/add-a-shortcut-to-the-home-screen-apd735880972/ios)
- [Apple: Use Shortcuts on iPhone](https://support.apple.com/guide/iphone/shortcuts-iph47e1c9d7d/ios)
- [Apple: Switch AirPods between Apple devices](https://support.apple.com/guide/airpods/dev228ba3df8/web)
