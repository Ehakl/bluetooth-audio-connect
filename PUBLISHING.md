# Publish the source on GitHub

This folder is a separate project. Publish this folder only, not its parent workspace or the original device-specific utility.

1. Review the MIT license, source, README limitations and validation status.
2. Create an empty public GitHub repository named `bluetooth-audio-connect` under your chosen account. Do not initialize it with another README or license.
3. In a terminal inside this folder:

```sh
git add src README.md LICENSE build.cmd "Connect Bluetooth Audio.cmd" .gitignore .gitattributes .github docs TESTING.md PUBLISHING.md CONTRIBUTING.md
git commit -m "Initial generic Bluetooth audio utility"
git remote add origin https://github.com/YOUR-ACCOUNT/bluetooth-audio-connect.git
git push -u origin main
```

Replace YOUR-ACCOUNT with the account you chose. Configure your preferred Git author identity first if needed. A source-only ZIP is also provided beside the project for review or manual upload; it excludes binaries, logs, settings and Git metadata.

The CI workflow checks compilation and offline tests, not Bluetooth hardware. Resolve the local security detection and complete hardware testing before publishing a downloadable executable or labeling a release stable. Do not advise users to bypass antivirus. No GitHub repository or release has been created by this preparation step.

