# DaisyTour editor implementation plan

Approved scope: a reusable tour styling editor, first integrated in the Gallery.
Use the existing checkout on `codex/daisytour-editor`, based on `main`. Do not commit or push.

## Scope and architecture

- Edit existing tour, step and annotation styles with a live preview.
- Keep the existing DaisyTour popup, renderer, layout and target tracking.
- Mount the inspector and handles inside the popup. Include the inspector bounds in placement decisions.
- Keep the editing preview card at the bottom beside the inspector. Start the inspector at up to 300 DIP and one third of the viewport. Allow resizing from its inner edge between 180 and 600 DIP while reserving space for the card.
- Keep application controls, preparation callbacks, content, bindings and resources intact.
- Use a draft style profile instead of writing evaluated values back into application dependency properties.
- Apply promotes the draft profile; cancel restores the previous active profile.
- Distinguish application value, explicit override, and inheritance past the current application scope.
- Use existing controls and color editor; support responsive layout, keyboard input, contrast themes and localization.
- Editing has its own completion lifecycle and must not mark a normal tour completed or skipped.

## A: Profile and resolution

- [x] Add typed versioned profile data and source-generated JSON serialization.
- [x] Address steps and annotations through stable IDs, never collection positions.
- [x] Support scalar style values, enums, solid colors, dash arrays, anchors, offsets, card and highlight settings.
- [x] Omitted properties use application settings; values override; an explicit inherit list skips the original value at that scope.
- [x] Reject unknown versions, members, enum values, invalid IDs, duplicate IDs, conflicting values/inherit entries and invalid numeric/color values.
- [x] Resolve profiles through the same renderer in playback and preview; preserve original bindings and custom objects.

## B: Editor session and inspector

- [x] Add reusable DaisyTourEditor and internal edit session.
- [x] Select existing steps and annotations; edit tour/step/annotation settings.
- [x] Add apply, discard, undo and redo with a draft profile.
- [x] Add mouse and keyboard panel resizing, plus localized icon-only Undo/Redo buttons; verify layout and input in the Gallery.
- [x] Integrate inspector into popup focus/input handling and placement.
- [x] Cancel preparation, release pointer capture and unsubscribe on close; ignore stale async work.

## C: Direct manipulation

- [x] Add annotation selection and keyboard-accessible handles.
- [x] Edit curvature, Bezier points, anchors/offsets, body width and arrowhead dimensions.
- [x] Reuse resolved geometry from the presenter; store connector-relative coordinates and DIP measurements.
- [x] Treat one drag as one undo operation; Escape cancels the active drag.
- [x] Keep handles aligned through reflow, scrolling and DPI changes.

## D: Gallery and persistence

- [x] Add a Tour Designer entry and a deterministic example tour with stable IDs.
- [x] Demonstrate straight/curved/Bezier/block arrows, speech bubbles and prepared/scrolled targets.
- [x] Gallery owns file dialogs; reusable library accepts profile objects and JSON.
- [x] Read and validate the entire import before replacing the draft as one undo operation.
- [x] Export a validated snapshot; report success only after writes and close complete.
- [x] Keep saving and applying separate. Cancellation and I/O errors preserve the draft.

## E: Verification and documentation

- [x] Test real JSON round trips, validation failures and inheritance at every scope.
- [x] Test live changes, undo/redo, apply/cancel, binding preservation, handles and lifecycle cleanup.
- [x] Test normal playback regressions and authoring-specific focus/outcomes.
- [x] Build with scripts/build_all.ps1 and test with scripts/test_windows.ps1, sequentially.
- [x] Inspect native Gallery behavior: keyboard, clipping, all five sizes, Light/Dark themes, narrow windows and DPI; verify forced high-contrast rendering through runtime tests.
- [x] Document embedding, supported JSON values, application-owned objects and limitations.
- [x] Review the complete diff for simplicity and correctness before delivery.

## Deferred scope

Full tour construction, target discovery/registry, serializing callbacks or arbitrary XAML, tapered arrow geometry and new gradient editing are separate future work.

## Progress

Plan saved before implementation. A-E are complete, with the unrelated full-suite
Inconclusive result and the high-contrast verification scope recorded below.
The current branch is `codex/daisytour-editor`, based directly on `main` (`6ae98e9`).
Changes from the unrelated toggle branch are excluded. No commit or push has been made.

Native UI checks have covered JSON file export/readback/import, Apply and normal replay,
step navigation, keyboard handle input, and Light/Dark screenshots at all five control sizes.
The final mouse check moved the Curvature handle by 85 physical pixels. One Undo restored
its original position and one Redo restored the edited position. Native JSON export/readback
confirmed the resulting curvature of 0.15496903219604657.
The bottom card and inspector were checked at 125% and 100% DPI, including a 1039 x 799
window, inspector docking on both sides and collapsing the inspector.
Evidence is in `TestResults/tour-editor-ui-20261022/17-final-curvature-drag.png` through
`20-collapsed.png` and `final-curvature-drag.json`.

The user subsequently authorized temporary focus changes for verification. UI actions
still use cua-driver, with background delivery first and foreground only when required.
The Gallery from the separate toggle checkout remains untouched.

The final regular build passed with the six existing Skia warnings:
`logs/all-build-debug-sdk10.0.402-20261022-162705-286.log`.
All 77 focused DaisyTour runtime tests passed on the final build, including contrast and bottom card placement:
`TestResults/Debug/20261022-162808-594-5fccd841/native-runtime-983975ca0b1d4096a2c2c8ee9d19e741/runtime-tests-windows.xml`.
The full runtime suite completed with 364 passed, zero failed and one Inconclusive:
`TestResults/Debug/20261022-162351-834-5352d363/native-runtime-74dd06c8e2d5461f9acafb511c6ed884/runtime-tests-windows.xml`.
The Inconclusive case is the existing derived-card blur capture test
`Given_DisplayDesignRegression.When_DerivedCardsEnableBlur_TheirTemplatesProvideTheBackdropLayer`;
its backdrop is intermittently unavailable in CI. It is outside DaisyTour.
High-contrast behavior is verified by runtime tests that drive the existing contrast event,
check forced renderer values and preserve authored styles; the system theme was not changed.
After the full suite, the final change reserved 16 DIP beside the inspector scrollbar.
The build and all 77 DaisyTour tests were repeated successfully for this layout change.
Final native screenshots confirm readable wrapped values and import status at 100% DPI,
plus the Extra Large control size at 125% DPI:
`21-final-light-narrow.png`, `22-final-import.png`, and `23-final-xl-125pct.png`
in the evidence folder above. Native import and Apply were repeated on this final build.
The Gallery theme and control size were restored to Dark and Medium.
Temporary pointer tracing has been removed from the source.

Follow-up: the inspector now has an inner-edge resize grip with a horizontal resize cursor,
and icon-only Undo/Redo buttons with localized tooltips and accessible names.
The regular build passed: `logs/all-build-debug-sdk10.0.402-20261022-170125-459.log`.
All 78 DaisyTour runtime tests passed:
`TestResults/Debug/20261022-170243-295-caf9888c/native-runtime-012f4e114b464d3ab1ec1377d75d4ab6/runtime-tests-windows.xml`.
The additional test covers width limits, card clearance, docking/collapse persistence,
unchanged profile/history during resizing, and real automation invocation of the icon buttons.
Native cua-driver checks confirmed a 100-pixel widening on the right and a 100-pixel
shrink on the left at 125% DPI, with the preview card remaining below the content.
The Undo/Redo icon buttons restored and reapplied a native curvature drag.
The Gallery is left open at CurvedArrow with the inspector on the right for user review.
Screenshot: `TestResults/tour-editor-ui-20261022/24-resizable-panel-icons.png`.

Follow-up: the curvature grip now sits beside the rendered arc midpoint with a fixed
clearance, rather than at its mathematical control point. Pointer movement preserves
the initial grab offset and moves the arc by the projected drag distance.
The regular build passed: `logs/all-build-debug-sdk10.0.402-20261022-170834-426.log`.
All 79 DaisyTour runtime tests passed:
`TestResults/Debug/20261022-170952-729-8b4c03ee/native-runtime-a6d315f4dbfa4f57848afa17f95ee4eb/runtime-tests-windows.xml`.
The added regression covers short/long horizontal, diagonal, reversed and vertical
connectors with negative, zero and positive curvature, plus the rendered response to movement.
Native cua-driver inspection confirmed the close grip position at 1400- and 2148-pixel
window widths. An off-center 40-pixel drag moved the grip exactly 40 pixels; Undo
restored its original position. Evidence: `25-curvature-wide.png` and
`26-curvature-narrow.png` in the same UI evidence folder.
The Gallery remains open at CurvedArrow in its original 2148 x 1251 window for review.
