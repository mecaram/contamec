export interface Account {
  id: number;
  name?: string | null;
  type?: string | null;
  subAccountRequires?: boolean | null;
  behaviorType?: string | null;
  active?: boolean | null;
}
