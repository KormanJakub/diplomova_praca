import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ThirdCardComponent } from './third-card.component';

describe('ThirdCardComponent', () => {
  let component: ThirdCardComponent;
  let fixture: ComponentFixture<ThirdCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ThirdCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ThirdCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
