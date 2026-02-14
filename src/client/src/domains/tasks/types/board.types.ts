export interface Column {
  id: string;
  name: string;
  position: number;
  wipLimit: number | null;
}

export interface Board {
  id: string;
  name: string;
  columns: Column[];
  createdAt: string;
}
