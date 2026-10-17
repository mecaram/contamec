export interface DashboardItem {
  name: string;
  icon: string;
  route: string;
  superAdminOnly?: boolean;
}

export interface DashboardCategory {
  title: string;
  items: DashboardItem[];
}
