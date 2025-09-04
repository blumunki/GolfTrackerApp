# CSS Architecture

## Overview

The Golf Tracker App uses a centralized CSS architecture designed for maintainability, consistency, and scalability. This document outlines the structure and conventions used throughout the application.

## Architecture Structure

```
wwwroot/css/
├── components/           # Component-specific styles
│   ├── golf-scorecard.css
│   ├── golf-dashboard.css
│   └── player-cards.css
├── layout/              # Layout-specific styles
│   ├── main-layout.css
│   └── navigation.css
├── themes/              # Theme variables and overrides
│   ├── golf-variables.css
│   └── mudblazor-overrides.css
└── utilities/           # Utility classes
    ├── typography.css
    └── spacing.css
```

## Design System

### Color Palette
The application uses a golf-inspired color scheme with CSS custom properties:
- **Primary**: Golf course green tones
- **Accent**: Sand trap and fairway colors
- **Neutral**: Clean grays and whites
- **Semantic**: Success (birdie), warning (bogey), error colors

### Typography
- **Primary Font**: System fonts for optimal performance
- **Headings**: Bold, clear hierarchy
- **Body Text**: Optimized for readability

### Spacing
- **Consistent Scale**: Based on 8px grid system
- **Component Spacing**: Logical spacing within components
- **Layout Spacing**: Consistent page and section spacing

## Component Styling

### Naming Conventions
- **Namespace**: All custom classes use `golf-` prefix
- **BEM-like**: Block__Element--Modifier pattern where appropriate
- **Descriptive**: Clear, meaningful class names

### Examples
```css
.golf-scorecard { }                 /* Component container */
.golf-scorecard-header { }          /* Component element */
.golf-score--eagle { }              /* Component modifier */
```

## MudBlazor Integration

The application leverages MudBlazor components with custom theming:
- **Base Components**: Use MudBlazor's comprehensive component library
- **Custom Overrides**: Minimal, targeted overrides for brand consistency
- **Theme Variables**: CSS custom properties for dynamic theming

## Responsive Design

- **Mobile-First**: Designed for mobile devices first
- **Breakpoints**: Consistent breakpoints across components
- **Flexible Layouts**: CSS Grid and Flexbox for adaptive layouts

## Performance

- **Bundled CSS**: All CSS files bundled by ASP.NET Core
- **Critical CSS**: Important styles loaded first
- **Optimized Selectors**: Efficient CSS selectors for performance

## Migration from CSS Isolation

The application was migrated from page-specific CSS isolation files to this centralized architecture for:
- **Better Maintainability**: Single source of truth for component styles
- **Design Consistency**: Shared variables and utilities
- **Reusability**: Styles can be shared across multiple pages
- **Theme Support**: Foundation for light/dark themes

## Best Practices

1. **Use CSS Custom Properties**: For values that might change or be themed
2. **Component-Based Styling**: Keep related styles together
3. **Minimal MudBlazor Overrides**: Leverage the framework's design system
4. **Semantic Class Names**: Clear, meaningful naming
5. **Mobile-First**: Design for mobile, enhance for desktop
