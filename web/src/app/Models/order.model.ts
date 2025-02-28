export interface Order {
  Id: number;
  Customizations: string[];
  TotalPrice: number;
  UserId: string;
  StatusOrder: number;
  PaymentId: string;
  PaymentStatus: string;
  CancellationToken: string;
  CreatedAt: string;
  UpdatedAt: string;
}
