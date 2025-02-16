export interface Size {
  Size: string;
  Quantity: number;
}

export interface Color {
  Name: string;
  FileId: string | null;
  PathOfFile: string | null;
  Sizes: Size[];
}

export interface Product {
  Id: string;
  TagId: string;
  Name: string;
  Description: string;
  Colors: Color[];
  Price: number;
  CreatedAt: string;
  UpdatedAt: string;
}
