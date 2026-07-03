import { Component } from '@angular/core';
import { FloatingCard } from "../floating-card/floating-card";

@Component({
  selector: 'app-hero',
  imports: [FloatingCard],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {}
