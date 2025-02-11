import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaleryCardComponent } from './galery-card.component';

describe('GaleryCardComponent', () => {
  let component: GaleryCardComponent;
  let fixture: ComponentFixture<GaleryCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaleryCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GaleryCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
