import {Design} from "./design.model";

export interface PairedDesign {
  Id: string;
  Name: string;
  DesignIds: string[];
  CreatedAt: string;
  UpdatedAt: string;
}

export interface AllPairedDesignsResponse {
  PairedDesign: PairedDesign[];
  Design: Design[];
}

export interface PairedDesignWithDesigns extends PairedDesign {
  Designs: Design[];
}
