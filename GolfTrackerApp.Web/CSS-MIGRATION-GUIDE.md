# CSS Architecture Migration Guide

## Overview
This document outlines the migration from page-specific CSS isolation to a centralized CSS architecture for better maintainability and consistency.

## Current Issues with CSS Isolation
1. **Scattered Styles**: Styles spread across multiple `.razor.css` files
2. **Code Duplication**: Similar styles duplicated across components
3. **Maintenance Overhead**: Changes require editing multiple files
4. **Design System Inconsistency**: No central place for theme variables

## New CSS Architecture

### Directory Structure
```
wwwroot/
├── css/
│   ├── components/
│   │   ├── golf-scorecard.css      # Golf scorecard component
│   │   ├── player-cards.css        # Player summary cards (future)
│   │   └── forms.css               # Form components (future)
│   ├── layout/
│   │   ├── navigation.css          # Navigation specific (future)
│   │   └── main-layout.css         # Layout specific (future)
│   └── themes/
│       └── golf-variables.css      # CSS custom properties (future)
└── app.css                         # Entry point importing all CSS
```

### Benefits
1. **Centralized Theming**: CSS custom properties for consistent colors
2. **Reusable Components**: Styles can be shared across multiple pages
3. **Better Organization**: Logical grouping of related styles
4. **Easier Maintenance**: Single source of truth for component styles
5. **Design System**: Foundation for consistent UI patterns

## Migration Steps

### Phase 1: Scorecard Component (Completed)
- ✅ Created `/wwwroot/css/components/golf-scorecard.css`
- ✅ Migrated scorecard styles with improved class naming
- ✅ Added CSS custom properties for theme consistency
- ✅ Updated `app.css` to import component styles

### Phase 2: Component Updates (Next)
1. Update `RoundDetails.razor` to use new CSS classes
2. Remove `RoundDetails.razor.css` file
3. Test for style consistency

### Phase 2: Layout Migration (COMPLETED ✅)
1. ✅ Created `/wwwroot/css/layout/main-layout.css` and `navigation.css`
2. ✅ Removed unused `MainLayout.razor.css` and `NavMenu.razor.css` files  
3. ✅ Updated import structure in `app.css`
4. ✅ Verified MudBlazor compatibility (components use MudDrawer/MudAppBar)

### Phase 3: Design System (COMPLETED ✅)
1. ✅ Created `/css/themes/golf-variables.css` with comprehensive design tokens
2. ✅ Created `/css/themes/mudblazor-overrides.css` for component theming
3. ✅ Implemented CSS custom properties for colors, spacing, typography
4. ✅ Added dark theme support foundation and utility classes

## CSS Class Naming Convention

### Golf Scorecard Classes
- **Container**: `.golf-scorecard`
- **Headers**: `.golf-scorecard-header`, `.golf-scorecard-player-header`
- **Cells**: `.golf-scorecard-cell`, `.golf-score-cell`
- **Score Types**: `.golf-score-eagle`, `.golf-score-birdie`, etc.
- **Sections**: `.golf-front-nine`, `.golf-back-nine`, `.golf-total-row`

### Benefits of New Naming
1. **Namespace Protection**: `golf-` prefix prevents conflicts
2. **Component Clarity**: Easy to identify component ownership
3. **Semantic Meaning**: Class names describe purpose
4. **BEM-like Structure**: Block-Element-Modifier pattern

## Implementation Notes

### CSS Custom Properties
```css
:root {
    --golf-primary: #3d7c3f;
    --golf-primary-dark: #2c5530;
    --golf-eagle: #e3f2fd;
    /* ... more variables */
}
```

### Responsive Design
All components include mobile-first responsive design with breakpoints at 768px.

### Print Styles
Golf scorecard includes print-optimized styles for physical scorecards.

## Testing Checklist
- ✅ Scorecard displays correctly on desktop
- ✅ Scorecard displays correctly on mobile  
- ✅ All score colors are applied correctly
- ✅ Print styles work properly
- ✅ No style conflicts with MudBlazor components
- ✅ Build succeeds with no CSS-related errors
- ✅ Theme variables properly integrated

## Future Enhancements
1. **SCSS Support**: Consider adding SCSS compilation for advanced features
2. **CSS-in-JS**: Evaluate CSS-in-JS solutions for dynamic theming
3. **Design Tokens**: Implement design token system for consistency
4. **Component Library**: Create reusable component CSS library
