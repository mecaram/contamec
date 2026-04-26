export interface DashboardItem {
  name: string;
  icon: string;
  route: string;
}

export interface DashboardCategory {
  title: string;
  items: DashboardItem[];
}
