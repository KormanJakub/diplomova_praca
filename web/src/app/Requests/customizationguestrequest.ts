import {CustomizationRequest} from "./customizationrequest";
import {GuestDataRequest} from "./guestdatarequest";

export interface CustomizationGuestRequest {
  GuestData: GuestDataRequest;
  Customizations: CustomizationRequest[];
}
