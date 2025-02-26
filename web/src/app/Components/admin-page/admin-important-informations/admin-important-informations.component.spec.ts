import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminImportantInformationsComponent } from './admin-important-informations.component';

describe('AdminImportantInformationsComponent', () => {
  let component: AdminImportantInformationsComponent;
  let fixture: ComponentFixture<AdminImportantInformationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminImportantInformationsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminImportantInformationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
