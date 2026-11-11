import { Component, input } from '@angular/core';

/** The company logo with the product name next to it. */
@Component({
  selector: 'app-brand',
  template: `
    <span class="brand" [class.brand-large]="size() === 'large'">
      <img class="brand-logo" src="ferre-logo.png" alt="Ferre" />
      <span class="brand-product">Link Manager</span>
    </span>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
      }

      .brand {
        display: inline-flex;
        align-items: center;
        gap: 12px;
      }

      .brand-logo {
        display: block;
        height: 26px;
        width: auto;
      }

      .brand-product {
        padding-left: 12px;
        border-left: 2px solid #c9cdea;
        font-weight: 600;
        font-size: 0.95rem;
        line-height: 1.2;
        color: var(--ferre-navy, #1b2164);
      }

      .brand-large .brand-logo {
        height: 36px;
      }

      .brand-large .brand-product {
        font-size: 1.05rem;
      }

      /* In the top bar of a phone only the logo is shown. */
      @media (max-width: 640px) {
        .brand:not(.brand-large) .brand-product {
          display: none;
        }
      }
    `,
  ],
})
export class Brand {
  readonly size = input<'small' | 'large'>('small');
}