# Two-developer workflow

`main` is the tested, working version of the TPMechanical.QAQC add-in.

For each change:

1. Pull the latest `main`.
2. Create a short-lived branch such as `feature/hanger-spacing` or `fix/ribbon-icon`.
3. Commit related changes with a clear message.
4. Push the branch and open a pull request.
5. Have the other developer review it before merging into `main`.
6. Delete the feature branch after it is merged.

Avoid intentionally editing the same method at the same time. If both developers need the same file, agree on separate sections first. Never commit `bin`, `obj`, `.vs`, user settings, or local Revit build/deployment output.

