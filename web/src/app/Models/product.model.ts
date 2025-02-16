interface Size {
  Size: string;
  Quantity: number;
}

interface Color {
  Name: string | null;
  FileId: string | null;
  PathOfFile: string | null;
  Sizes: Size[];
}

interface Product {
  Id: string;
  TagId: string;
  Name: string;
  Description: string;
  Colors: Color[];
  Price: number;
  CreatedAt: string;
  UpdatedAt: string;
}
