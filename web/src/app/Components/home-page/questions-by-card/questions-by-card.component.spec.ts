import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QuestionsByCardComponent } from './questions-by-card.component';

describe('QuestionsByCardComponent', () => {
  let component: QuestionsByCardComponent;
  let fixture: ComponentFixture<QuestionsByCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuestionsByCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(QuestionsByCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
