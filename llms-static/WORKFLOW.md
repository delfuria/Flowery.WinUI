# Supplementary Documentation Workflow

This document provides step-by-step instructions for creating supplementary documentation files for Flowery.WinUI controls. Follow these steps exactly.

---

## TASK OVERVIEW

You are creating markdown files that provide **human-friendly explanations** of WinUI 3 controls. These files enhance the auto-generated technical documentation with:

- Plain-English descriptions of what each variant/option does
- Visual/behavioral descriptions (since you can't see the actual UI)
- Tables explaining enum values and their effects
- Practical usage examples
- Tips and best practices

**Output location:** `llms-static/` folder
**File naming:** `DaisyControlName.md` (must match the C# class name exactly!)

---

## STEP 1: Identify the Control

Before writing documentation, you need to gather information from these sources:

### 1.1 Read the C# Control File

**Location:** `Flowery.WinUI/Controls/DaisyControlName.cs`

Extract:

- Class name (e.g., `DaisyButton`)
- Base class (what it inherits from)
- Native `DependencyProperty.Register` and `RegisterAttached` definitions, including their CLR accessors
- All enum definitions (usually at the top of the file)
- XML documentation comments (`/// <summary>`)

### 1.2 Read the XAML Theme File (if exists)

**Location:** `Flowery.WinUI/Themes/DaisyControlName.xaml`

Look for:

- Visual structure (what elements make up the control)
- Animations (look for `<Animation>`or`<Storyboard>` tags)
- Style variations (different appearances based on properties)

### 1.3 Read the Example File

**Location:**`Flowery.WinUI.Gallery.Core/Examples/*Examples.xaml`

Find the section for this control by searching for:

- `SectionId="controlname"` in a SectionHeader
- `<controls:DaisyControlName` elements

Extract:

- How the control is used in practice
- Which property combinations are shown
- Common patterns

---

## STEP 2: Determine What Needs Documentation

**Ask these questions:**

| Question | If YES → Document |
| --- | --- |
| Does it have multiple **variants** (enum with visual differences)? | Explain what each variant looks like/does |
| Does it have **size** options? | Table with sizes and use cases |
| Does it have **color** options? | Table with color meanings |
| Does it have **behavior modes**? | Explain when to use each |
| Is it a **container** for other controls? | Show composition patterns |
| Does it have **animations**? | Describe timing and visual effect |
| Are there **non-obvious usage patterns**? | Add tips/best practices |

**Skip creating a supplementary doc if:**

- The control is very simple (just a styled wrapper)
- The property names are self-explanatory
- There are no variants, just basic properties

---

## STEP 3: Write the Documentation

### 3.1 File Header (Required)

```markdown
<!-- Supplementary documentation for DaisyControlName -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->
```

### 3.2 Overview Section (Required)

Write a brief paragraph explaining:

- What the control does
- Key features (number of variants, size options, etc.)
- When to use this control

**Example:**

```markdown
# Overview

DaisyButton provides color variants, button styles, and five size options.
It supports icons and actions such as navigation or form submission.
Read the current enum declarations before you document variant counts.
```

### 3.3 Variant Tables (If Applicable)

For each enum property that affects appearance or behavior:

```markdown
## Variant Options

| Variant | Description |
| --- | --- |
| **Default** | Standard appearance, inherits theme styling |
| **Primary** | High emphasis, main actions |
| **Secondary** | Medium emphasis, alternative actions |
```

**Guidelines for descriptions:**

- Describe the VISUAL appearance or BEHAVIOR, not just restate the name
- Use concrete terms: "rounded corners", "bold text", "subtle shadow"
- Compare to familiar patterns: "like a tooltip", "similar to a badge"

### 3.4 Size Tables (If Applicable)

```markdown
## Size Options

| Size | Approximate Dimensions | Use Case |
| --- | --- | --- |
| ExtraSmall | ~16px height | Inline text, compact UIs |
| Small | ~20px height | Toolbars, secondary actions |
| Medium | ~24px height | Default, general purpose |
| Large | ~32px height | Primary actions, emphasis |
| ExtraLarge | ~40px height | Hero sections, full-width |
```

### 3.5 Color Tables (If Applicable)

```markdown
## Color Options

| Color | Semantic Meaning |
| --- | --- |
| Default | Inherits from theme (usually base-content) |
| Primary | Brand color, main actions |
| Secondary | Alternative brand color |
| Accent | Highlight, special emphasis |
| Neutral | Muted, low emphasis |
| Info | Informational (typically blue) |
| Success | Positive outcomes (typically green) |
| Warning | Caution needed (typically yellow/orange) |
| Error | Problems, destructive actions (typically red) |
```

### 3.6 Quick Examples (Required)

Show practical XAML usage:

```markdown
## Quick Examples

\`\`\`xml
<!-- Basic usage -->
<controls:DaisyButton Content="Click me" />

<!-- With variant -->
<controls:DaisyButton Content="Save" Variant="Primary" />

<!-- With size -->
<controls:DaisyButton Content="Small" Size="Small" />

<!-- Combining options -->
<controls:DaisyButton Content="Delete" Variant="Error" Size="Large" />
\`\`\`
```

**Guidelines for examples:**

- Start with simplest usage
- Progress to more complex combinations
- Use realistic Content/text values
- Show 4-8 examples (not too few, not overwhelming)

### 3.7 Additional Sections (As Needed)

**For controls with animations:**

```markdown
## Animation Timing

| Animation | Duration | Notes |
| --- | --- | --- |
| Fade in | 0.2s | On hover |
| Slide | 0.3s | Panel transition |
```

**For container controls:**

```markdown
## Content Structure

Use native layout controls to organize content inside DaisyCard:

\`\`\`xml
<controls:DaisyCard>
    <StackPanel Spacing="12">
        <TextBlock Text="Title" FontSize="20" />
        <TextBlock Text="Card content here" />
        <controls:DaisyButton Content="Action" />
    </StackPanel>
</controls:DaisyCard>
\`\`\`
```

**For controls with special behaviors:**

```markdown
## Tips & Best Practices

- Use `IsEnabled="False"` to disable interaction while preserving visual state
- Combine with `DaisyTooltip` for additional context
- Use `ToolTipService.ToolTip` to add a native WinUI 3 tooltip
```

---

## STEP 4: Verify the Output

### 4.1 Checklist

- [ ] File is named exactly `DaisyControlName.md` (matches C# class name)
- [ ] File is in `llms-static/` folder
- [ ] Has HTML comment header
- [ ] Has `## Overview` section
- [ ] Tables have `|` aligned properly (though not strictly required)
- [ ] Code examples use `controls:` namespace prefix
- [ ] Ends with `---`

### 4.2 Common Mistakes to Avoid

| Mistake | Correct Approach |
| --- | --- |
| "Primary variant makes it primary" | "Primary variant uses the brand color with high visual emphasis" |
| Copying XML comments verbatim | Expand and explain in human terms |
| Missing namespace in examples | Always use `controls:DaisyControlName` |
| Documenting every property | Focus on variants/enums that need explanation |
| Very long descriptions | Keep table cells to 1-2 sentences max |

---

## COMPLETE EXAMPLE

Here is a complete supplementary doc for reference:

**File:** `llms-static/DaisyBadge.md`

```markdown
<!-- Supplementary documentation for DaisyBadge -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

## Overview

DaisyBadge displays status labels and counts.
Use `Variant` for the color, `IsOutline` for an outlined border, and `Size` for one of five sizes.

## Appearance

| Property | Description |
| --- | --- |
| `IsOutline="False"` | Filled background with contrasting text |
| `IsOutline="True"` | Transparent background with a colored border |
| `Variant="Ghost"` | Muted background and text |

## Size Options

| Size | Use Case |
| --- | --- |
| ExtraSmall | Notification dots, minimal indicators |
| Small | Inline with text, compact layouts |
| Medium | Default, general purpose |
| Large | Emphasized badges, headers |
| ExtraLarge | Hero sections, large callouts |

## Quick Examples

\`\`\`xml
<!-- Basic badge -->
<controls:DaisyBadge Content="New" />

<!-- Colored badges -->
<controls:DaisyBadge Content="3" Variant="Primary" />
<controls:DaisyBadge Content="Error" Variant="Error" />
<controls:DaisyBadge Content="Done" Variant="Success" />

<!-- Style variants -->
<controls:DaisyBadge Content="Outline" IsOutline="True" Variant="Primary" />
<controls:DaisyBadge Content="Ghost" Variant="Ghost" />

<!-- Inside a button -->
<controls:DaisyButton>
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="Messages" />
        <controls:DaisyBadge Content="5" Variant="Error" Size="Small" />
    </StackPanel>
</controls:DaisyButton>

<!-- Empty badge (just a dot) -->
<controls:DaisyBadge Variant="Success" Size="ExtraSmall" />
\`\`\`

## Tips

- Use `ExtraSmall` with no content for notification dots
- Pair the `Error` variant with counts for unread or urgent items
- `IsOutline="True"` works well on colored backgrounds

---
```

---

## CONTROLS THAT LIKELY NEED SUPPLEMENTARY DOCS

Based on complexity, these controls would benefit most:

### High Priority (Multiple Variants/Complex)

- DaisyButton (variants, styles, sizes, states)
- DaisyAlert (types, styles, icons)
- DaisyBadge (styles, sizes)
- DaisyCard (structure, variants)
- DaisyModal (placement, behavior)
- DaisyToast (positions, types)
- DaisyDrawer (placement, behavior)
- DaisyTabs (styles, behavior)
- DaisyProgress (styles, states)
- DaisyMenu (structure, states)

### Medium Priority (Some Variants)

- DaisyCheckBox (sizes, colors)
- DaisyToggle (sizes, colors)
- DaisyRadio (sizes, colors)
- DaisyInput (styles, sizes, states)
- DaisySelect (styles, sizes)
- DaisyAvatar (shapes, sizes)
- DaisyDivider (orientations, styles)
- DaisySteps (styles, states)
- DaisyTimeline (layout options)

### Lower Priority (Simple/Self-Explanatory)

- DaisyKbd (keyboard key display)
- DaisySkeleton (loading placeholder)
- DaisyStat (statistics display)

---

## EXECUTION STEPS FOR AI ASSISTANT

1. **List controls** needing documentation:

    ```txt
    List files in Flowery.WinUI/Controls/ matching Daisy*.cs
    ```

2. **For each control**, in order:

   a. Read the C# file
   b. Read the theme XAML file (if exists)
   c. Search for examples in `Flowery.WinUI.Gallery.Core/Examples/`
   d. Determine if supplementary docs are needed (use criteria above)
   e. If needed, write the markdown file

3. **Output** each file to `llms-static/DaisyControlName.md`

4. **After all files are created**, the user will run:

   ```powershell
   python Utils/generate_docs.py
   python Utils/generate_site.py
   ```

---

## FINAL NOTES

- **Quality over quantity**: A well-written doc for complex controls is better than mediocre docs for everything
- **Be descriptive**: You can't see the UI, so describe what users will experience
- **Be practical**: Focus on what helps someone USE the control
- **Match the style**: Look at `llms-static/DaisyLoading.md` as the gold standard
