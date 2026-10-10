/**
 * Policy names, identical to the backend constants (docs/architecture.md §4–5).
 * Checked in three places: menu (`requiredPolicy`), route (`permissionGuard`)
 * and element (`*appPermission`).
 */
export const Permissions = {
  Dashboard: {
    ViewBalance: 'Permissions.Dashboard.ViewBalance',
  },
  Players: {
    ViewPlayer: 'Permissions.Players.ViewPlayer',
    ManagePlayer: 'Permissions.Players.ManagePlayer',
  },
  Matches: {
    ViewMatch: 'Permissions.Matches.ViewMatch',
  },
  Content: {
    ManageLessons: 'Permissions.Content.ManageLessons',
  },
} as const;

export const ALL_PERMISSIONS: string[] = Object.values(Permissions).flatMap(
  (area) => Object.values(area as Record<string, string>)
);
