import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BestSaleComponent } from './best-sale.component';

describe('BestSaleComponent', () => {
  let component: BestSaleComponent;
  let fixture: ComponentFixture<BestSaleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BestSaleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BestSaleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
