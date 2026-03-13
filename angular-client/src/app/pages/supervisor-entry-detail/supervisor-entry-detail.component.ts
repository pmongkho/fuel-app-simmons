import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-supervisor-entry-detail',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './supervisor-entry-detail.component.html',
  styleUrl: './supervisor-entry-detail.component.css',
})
export class SupervisorEntryDetailComponent {
  reason = '';
}
