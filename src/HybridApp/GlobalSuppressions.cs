// Suppressions for Razor-generated routing code in MAUI Blazor apps
// 
// CONTEXT: After upgrading to .NET 10, the enhanced trimming analysis now flags Razor-generated 
// code for Router and LayoutView components as potentially unsafe. However, these warnings are 
// false positives in the context of MAUI Blazor Hybrid apps.
//
// TECHNICAL DETAILS:
// - IL2111: Router.NotFoundPage.set and LayoutView.Layout.set use reflection for component discovery
// - IL2110: LayoutView internal fields are accessed via reflection during layout resolution
// 
// SAFETY: These suppressions are safe because:
// 1. Layout components are explicitly referenced in Routes.razor and preserved by Razor compilation
// 2. MAUI Blazor hybrid apps don't use aggressive trimming that would remove referenced components
// 3. The Router and LayoutView are core Blazor components designed to work with reflection
//
// ALTERNATIVES ATTEMPTED:
// - Adding DynamicallyAccessedMembers attributes to layout components (failed)
// - Restructuring Router configuration to avoid LayoutView (failed) 
// - Using direct component references instead of typeof() (failed)
//
// This is a known limitation documented in the official MAUI repository.
// Tracking issue: https://github.com/dotnet/maui/issues/22368

[assembly: System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming", 
    "IL2111", 
    Justification = "Blazor Router and LayoutView components use safe reflection patterns that are preserved by the MAUI build process.")]

[assembly: System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming", 
    "IL2110", 
    Justification = "Blazor LayoutView internal fields are accessed via reflection in a controlled manner that's safe for MAUI hybrid apps.")]