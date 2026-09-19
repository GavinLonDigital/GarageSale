---
name: live-visual-redesign
description: >
  Perform a dramatic presentation-only visual redesign of the GarageSale
  frontend during a live Hot Reload demonstration. Use when the user asks
  to redesign, restyle, reskin, visually transform, or explore a different
  visual direction for GarageSale without changing its functionality.
---

# GarageSale Live Visual Redesign

## Objective

Transform the visual design of the existing GarageSale application while
preserving how the application works.

This workflow is intended for a live demonstration in which the application
is already running and the user may be watching the interface change in the
browser as edits are made.

The redesign should therefore be both visually ambitious and minimally
disruptive to the running application.

## Core rule

CHANGE THE DESIGN, NOT THE FUNCTIONALITY.

Preserve existing:

- application behavior
- business logic
- routes
- navigation destinations
- data and data bindings
- authentication behavior
- event handlers
- buttons and controls
- user interactions

Do not add application features simply to support the new design.

## Creative intent

Treat the user's design prompt as an art direction rather than a checklist
of literal CSS changes.

Use your own design judgment to create a coherent visual system from that
direction.

The transformation should be substantial and immediately noticeable.

Do not settle for a simple color swap or superficial theme.

Consider the interface as a complete composition, including:

- typography
- color
- visual hierarchy
- spacing and rhythm
- page proportions
- navigation
- hero treatment
- product presentation
- grid and layout
- imagery
- borders and shapes
- shadows and depth
- responsive behavior
- hover and focus states
- transitions and restrained motion

Make these decisions as a coherent design system rather than as unrelated
styling changes.

## Live development constraint

The application is already running in Docker using dotnet watch and
Hot Reload.

Keeping the running application and browser experience stable during the
redesign is an important goal.

Do not restart or rebuild the development environment merely to apply
visual changes.

Do not run:

- docker compose down
- docker compose up
- docker compose restart
- docker restart
- dotnet run
- another dotnet watch process

Do not modify Docker configuration, Docker Compose, development
entrypoints, Hot Reload configuration, project configuration, or
development infrastructure as part of a visual redesign.

The development environment is already configured.

## Implementation strategy

Before making changes, briefly inspect the existing frontend to understand:

1. the visible page structure
2. the relevant Razor components/layouts
3. the existing stylesheets
4. the existing CSS classes and selectors
5. the smallest useful set of frontend files for the redesign

Then implement the redesign.

For this live workflow, prefer the least disruptive technique capable of
producing the desired visual result.

Prefer, roughly in this order:

1. CSS and existing stylesheets
2. existing classes and selectors
3. CSS Grid and Flexbox
4. typography, spacing, sizing and positioning
5. pseudo-elements and other CSS presentation techniques
6. presentation-only Razor or layout markup changes where they materially
   improve the result

This is a preference, not a prohibition against Razor changes.

Razor components and layout files may be modified when useful for the
visual design.

When modifying Razor markup, preserve existing bindings, event handlers,
component parameters, navigation, controls and behavior.

Avoid modifying C# logic, @code blocks, services, models, or backend code
for purely visual purposes.

Avoid large structural component changes when an equivalent visual result
can be achieved through styling or small presentation-only markup changes.

## Hot Reload friendliness

Work in coherent changes and avoid leaving Razor markup temporarily broken
while editing.

Prefer changes that can be applied by the existing Hot Reload workflow
without restarting the running Blazor Server application.

If a visual idea would require a disruptive application or architectural
change, find a visually strong presentation-layer alternative instead.

Do not sacrifice the quality of the redesign unnecessarily. CSS-first
does not mean visually conservative.

Modern CSS can be used aggressively to transform the existing interface.

## Responsive design

The redesign must remain usable at different viewport sizes.

Where appropriate, account for:

- desktop
- tablet
- mobile
- arbitrary product counts
- long product names
- image aspect ratios
- navigation wrapping/collapse

Do not optimize exclusively for the currently visible browser size.

## Accessibility

Maintain reasonable:

- text contrast
- control visibility
- keyboard focus states
- readability
- interactive affordances

When introducing animation or significant motion, respect
prefers-reduced-motion.

## Final quality pass

After implementing the main transformation, inspect the changed frontend
as a designer as well as an engineer.

Look for:

- inconsistent spacing
- weak visual hierarchy
- accidental framework/default styling
- awkward image cropping
- inconsistent typography
- unnecessary borders
- inconsistent radii
- alignment problems
- weak hover/focus states
- responsive problems
- unfinished-looking details

Correct these issues within the presentation layer.

Do not expand the quality pass into unrelated refactoring or functional
changes.

## Success criteria

The redesign succeeds when:

1. GarageSale is immediately recognizable as having a substantially
   different visual identity.
2. The design feels coherent rather than being a collection of CSS effects.
3. Existing application functionality continues to work.
4. The implementation remains focused on the presentation layer.
5. The existing live development environment has not been unnecessarily
   restarted or modified.