import { Component } from '@angular/core';
import { DiceService, DiceRoll } from '../Services/dice.service';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';

interface DiceGroupView {
  total: number;
  expression: string;
  timestamp: number;
}

@Component({
  selector: 'app-dice',
  imports: [MatButtonModule, MatIconModule, MatBadgeModule],
  templateUrl: './dice.html',
  styleUrl: './dice.css',
})
export class Dice {
  lastRoll?: DiceRoll;
  historyView: DiceGroupView[] = [];
  menuOpen = false;
  diceOptions: number[] = [2, 4, 6, 10, 12, 20, 100];

  // selection counts per die (e.g., { 100: 2, 2: 3 })
  selectedCounts: Record<number, number> = {};

  constructor(private diceService: DiceService) {}

  toggleMenu() {
    this.menuOpen = !this.menuOpen;
  }

  // FAB click: open menu, or roll current selection if open
  onFabClick() {
    if (!this.menuOpen) {
      this.menuOpen = true;
      return;
    }
    const requests = Object.entries(this.selectedCounts).map(([sides, count]) => ({
      sides: +sides,
      count: count as number,
    }));
    if (requests.length) {
      this.rollSelection(requests);
    } else {
      this.menuOpen = false;
    }
  }

  rollDice(sides: number = 100) {
    this.lastRoll = this.diceService.rollInstant(sides);
    this.pushGroupView([{ sides, count: 1 }], [this.lastRoll.timestamp], [this.lastRoll.result]);
    this.menuOpen = false;
  }

  // Increment count on left-click
  addDie(sides: number) {
    this.selectedCounts[sides] = (this.selectedCounts[sides] ?? 0) + 1;
  }

    // Decrement on right-click
  removeDie(sides: number) {
    if (!this.selectedCounts[sides]) return;
    const next = this.selectedCounts[sides] - 1;
    if (next <= 0) {
      delete this.selectedCounts[sides];
    } else {
      this.selectedCounts[sides] = next;
    }
  }

  private rollSelection(requests: Array<{ sides: number; count: number }>) {
    const rolls = this.diceService.rollMany(requests);
    // lastRoll for convenience/debug
    this.lastRoll = rolls[rolls.length - 1];

    // Compute total and a readable expression like "2d100 + 3d2"
    this.pushGroupView(
      requests,
      rolls.map(r => r.timestamp),
      rolls.map(r => r.result)
    );

    // Reset selection and close menu
    this.selectedCounts = {};
    this.menuOpen = false;
  }

  private pushGroupView(
    requests: Array<{ sides: number; count: number }>,
    timestamps: number[],
    results: number[]
  ) {
    const total = results.reduce((a, b) => a + b, 0);
    const expression = requests
      .map(r => `${r.count}d${r.sides}`)
      .join(' + ');
    const timestamp = timestamps[0] ?? Date.now();

    const group: DiceGroupView = { total, expression, timestamp };

    // Keep only last 5 groups
    this.historyView = [...this.historyView, group].slice(-5);
  }
}
