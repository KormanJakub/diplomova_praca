export interface GuestOrderRequest {
  GuestUserId: string;
  CustomizationsId: string[];
  PaymentMethod?: string;
  DeliveryMethod?: string;
  PacketaPointId?: string;
  PacketaPointName?: string;
  PacketaPointAddress?: string;
}
