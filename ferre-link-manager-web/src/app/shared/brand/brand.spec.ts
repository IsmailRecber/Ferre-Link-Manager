import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { Brand } from './brand';

describe('Brand', () => {
  it('shows the company logo with an alternative text and the product name', async () => {
    await TestBed.configureTestingModule({ imports: [Brand] }).compileComponents();

    const fixture = TestBed.createComponent(Brand);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const logo = root.querySelector('img');

    expect(logo?.getAttribute('alt')).toBe('Ferre');
    expect(logo?.getAttribute('src')).toBe('ferre-logo.png');
    expect(root.textContent).toContain('Link Manager');
  });

  it('is larger when asked to be', async () => {
    await TestBed.configureTestingModule({ imports: [Brand] }).compileComponents();

    const fixture = TestBed.createComponent(Brand);
    fixture.componentRef.setInput('size', 'large');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.brand-large')).not.toBeNull();
  });
});