export interface Order {
  Id: number;
  Customizations: string[];
  TotalPrice: number;
  UserId: string;
  StatusOrder: number;
  PaymentId: string;
  PaymentStatus: string;
  PaymentMethod?: string;
  PaymentFee?: number;
  DeliveryMethod?: string;
  PacketaPointId?: string;
  PacketaPointName?: string;
  PacketaPointAddress?: string;
  CancellationToken: string;
  FollowToken: string;
  CreatedAt: string;
  UpdatedAt: string;
}
