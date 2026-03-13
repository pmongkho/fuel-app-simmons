import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-supervisor-entries',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './supervisor-entries.component.html',
  styleUrl: './supervisor-entries.component.css',
})
export class SupervisorEntriesComponent {}
