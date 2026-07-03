import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Landing } from "./Pages/landing/landing";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('RafiqAngular');
}
