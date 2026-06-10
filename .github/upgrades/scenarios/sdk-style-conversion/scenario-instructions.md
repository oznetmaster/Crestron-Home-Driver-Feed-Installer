# SDK-style Project Conversion

## Strategy
Validate the existing project files and document that no SDK-style conversion is required.

## Preferences
- **Flow Mode**: Automatic
- **Commit Strategy**: After Each Task

## Custom Instructions
- Do not modify the current `NugetForCrestronDrivers` solution for this cross-repository workflow task, except for transient execution artifacts strictly needed to run the plan.
- Use spaces only in YAML files; do not insert tab characters.
- Inspect and update package metadata in the WiserHeatCrestronDriver, WeatherLinkLiveCrestronDriver, and OverkizCrestronDriver project files so NuGet packages publish cleanly.
- NuGet packages must not contain the driver DLL; they should contain only the final `.pkg` artifact, which remains the release asset.
- Validation for NuGet packaging must use the `.pkg` files produced by Release builds, not Debug outputs.
- Commit, push, and create a new tagged release for each external driver repository.
- Update the three external repository README files to mention NuGet package availability before commit and release work.
- Do not stop until the README update work is complete.
- Publish the NuGet packages for each external driver repository.
- Use a new tag for each external driver repository, choosing an appropriate minor-version bump from the existing current tag.
- NuGet search in the current solution should only return packages whose payload contains a `.pkg`, with an optional filter for packages whose title includes `Crestron`.
- Treat this driver package format as a new category with a custom publishing standard controlled by the user.
- Define and adopt a v1 custom publishing standard for Crestron driver NuGet packages before updating the external driver repositories to conform to it.
- Create a README in this project that defines the custom Crestron driver NuGet publishing standard.
- Proceed without stopping unless there is a problem.
- Do not stop until the current manifest-inclusion fix plan is finished.
- Commit, push, and create new tagged releases for the three external driver repositories after the v1 standard updates.
- Do not stop until the current release plan is completed.
- The current project's NuGet search should include a checkbox to only search for v1 compliant packages, and it should default to enabled.
- Use strict package inspection as the final authority for v1 compliance filtering rather than relying on package type metadata alone.
- Update the three external driver README files to mention that their NuGet packages conform to Crestron Driver NuGet Publishing Standard v1.
- Note in both this repository README and the three external driver READMEs that the v1 standard is not an official Crestron specification; it is an open source packaging standard created to facilitate community distribution of Crestron Home drivers.
- Do not use one umbrella package type; distinguish between Crestron Home drivers and custom Crestron system drivers as separate package categories.
- Use `CrestronHomeDriver` for the current v1 standard instead of `CrestronDriver`.
- Complete the current CrestronHomeDriver rename plan end-to-end, including committing, pushing, and releasing the three external driver repositories after the updates.
- Rename the three external package IDs to the recommended CrestronHomeDriver format, republish them immediately, and if possible unlist the old package IDs.
- Each driver README should explicitly note the published NuGet package name.
- The current search implementation must inspect more candidate packages or page through more search results because WeatherLinkLive still does not appear even though NuGet.org shows it as indexed and listed.
- Each entry in the package list should have an info button that opens more information about that entry.
- Add UDP broadcast-based Crestron processor discovery, merge discovered processors into the dropdown, keep manual entry available, and use a short asynchronous timeout with deduplication by IP.
- Treat all warnings as errors in Debug builds for the current project so they are easier to catch and fix.
- Make generated code comply with the repository `.editorconfig`, and follow those rules going forward.
- Make discovery expose debugging output and display error states in red so failures are visible.
- Make discovery diagnostics more detailed instead of guessing, and use an ephemeral local UDP port for discovery.
- Filter discovered Crestron Home processors by model string, not hostname, and only include CP4-R, MC4-R, DIN-AP4-R, and MC4-R-I.
- Display discovered processor entries in the list by hostname and address (in either order), even though filtering is based on model string.
- The processor port field is not needed and should be removed from the UI flow.
- The NuGet feed box can be shorter to allow more space for package results.
- The package details panel should refer to `crestron-driver-package.json`, not `drivers.json`.
- Selected processors should remain visible in the dropdown after selection.
- Discovered processors should be persisted so discovery does not need to be rerun every time.
- Cached packages should be visible and reusable across runs, with delete support.
- The discovery diagnostics panel should be optional via a checkbox so the space can be used by the other lists.
- Avoid using NuGet as product branding; rename the app/repo to a neutral feed-based installer name that does not imply affiliation with Crestron's approved driver feed.
- Use `CrestronHomeDriverFeedInstaller` as the new current-app branding name.
