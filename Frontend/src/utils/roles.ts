export function getHomeRouteForRole(role: string | null | undefined): string {
  switch ((role || '').toLowerCase()) {
    case 'admin':
      return '/admin'
    case 'mentor':
      return '/mentor-dashboard'
    default:
      return '/dashboard'
  }
}