# Chart Integration Audit

## Current checkpoint

- `chart-attribution.tsx` exists under `apps/web/src/components/terminal/`.
- The reusable attribution component is prepared but no chart renderer consumer has been identified yet.
- Terminal components are currently organized separately from the chart engine layer.

## Next implementation step

The next chart terminal change should be made at the actual renderer mount point:

1. Locate the chart canvas / renderer component.
2. Import `ChartAttribution` there.
3. Keep attribution coupled to the rendered chart, not sidebar or inspector UI.
4. Validate TypeScript and production build before further chart features.

This prevents unused UI components and keeps the terminal architecture modular.
