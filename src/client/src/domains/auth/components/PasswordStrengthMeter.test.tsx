import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import * as fc from 'fast-check';
import {
  evaluatePasswordStrength,
  PasswordStrengthMeter,
  type StrengthLevel,
} from './PasswordStrengthMeter';

describe('evaluatePasswordStrength', () => {
  it('should return score 0 for empty string', () => {
    const result = evaluatePasswordStrength('');
    expect(result.score).toBe(0);
    expect(result.level).toBe('tooWeak');
  });

  it('should detect lowercase letters', () => {
    const result = evaluatePasswordStrength('a');
    const check = result.checks.find((c) => c.key === 'hasLowercase');
    expect(check?.passed).toBe(true);
  });

  it('should detect uppercase letters', () => {
    const result = evaluatePasswordStrength('A');
    const check = result.checks.find((c) => c.key === 'hasUppercase');
    expect(check?.passed).toBe(true);
  });

  it('should detect digits', () => {
    const result = evaluatePasswordStrength('1');
    const check = result.checks.find((c) => c.key === 'hasDigit');
    expect(check?.passed).toBe(true);
  });

  it('should detect special characters', () => {
    const result = evaluatePasswordStrength('!');
    const check = result.checks.find((c) => c.key === 'hasSpecial');
    expect(check?.passed).toBe(true);
  });

  it('should detect minimum length of 8', () => {
    expect(evaluatePasswordStrength('1234567').checks.find((c) => c.key === 'minLength')?.passed).toBe(false);
    expect(evaluatePasswordStrength('12345678').checks.find((c) => c.key === 'minLength')?.passed).toBe(true);
  });

  it('should detect long password bonus (12+)', () => {
    expect(evaluatePasswordStrength('12345678901').checks.find((c) => c.key === 'longPassword')?.passed).toBe(false);
    expect(evaluatePasswordStrength('123456789012').checks.find((c) => c.key === 'longPassword')?.passed).toBe(true);
  });

  it('should return veryStrong for password meeting all 6 criteria', () => {
    const result = evaluatePasswordStrength('MyP@ssword123');
    expect(result.score).toBe(6);
    expect(result.level).toBe('veryStrong');
  });

  it('should return strong when 5 of 6 criteria met', () => {
    // All required + one bonus (long but no special)
    const result = evaluatePasswordStrength('MyPassword12');
    expect(result.score).toBe(5);
    expect(result.level).toBe('strong');
  });

  it('should return fair when all required met but no bonus', () => {
    const result = evaluatePasswordStrength('MyPass1x');
    expect(result.score).toBe(4);
    expect(result.level).toBe('fair');
  });

  it('should return weak when only some required criteria met', () => {
    // Only lowercase + length
    const result = evaluatePasswordStrength('abcdefgh');
    expect(result.level).toBe('weak');
  });

  it('should return tooWeak for very short passwords', () => {
    const result = evaluatePasswordStrength('a');
    expect(result.level).toBe('tooWeak');
  });

  it('should have exactly 4 required and 2 bonus checks', () => {
    const result = evaluatePasswordStrength('test');
    expect(result.checks.filter((c) => c.required)).toHaveLength(4);
    expect(result.checks.filter((c) => !c.required)).toHaveLength(2);
  });

  it('property: score equals number of passed checks', () => {
    fc.assert(
      fc.property(fc.string({ minLength: 0, maxLength: 30 }), (password) => {
        const result = evaluatePasswordStrength(password);
        expect(result.score).toBe(result.checks.filter((c) => c.passed).length);
      }),
    );
  });

  it('property: score is between 0 and 6', () => {
    fc.assert(
      fc.property(fc.string({ minLength: 0, maxLength: 50 }), (password) => {
        const result = evaluatePasswordStrength(password);
        expect(result.score).toBeGreaterThanOrEqual(0);
        expect(result.score).toBeLessThanOrEqual(6);
      }),
    );
  });

  it('property: level is always a valid StrengthLevel', () => {
    const validLevels: StrengthLevel[] = ['tooWeak', 'weak', 'fair', 'strong', 'veryStrong'];
    fc.assert(
      fc.property(fc.string({ minLength: 0, maxLength: 50 }), (password) => {
        const result = evaluatePasswordStrength(password);
        expect(validLevels).toContain(result.level);
      }),
    );
  });

  it('property: longer passwords never have lower score than shorter prefixes', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1, maxLength: 20 }),
        fc.string({ minLength: 1, maxLength: 10 }),
        (base, extra) => {
          const shortResult = evaluatePasswordStrength(base);
          const longResult = evaluatePasswordStrength(base + extra);
          expect(longResult.score).toBeGreaterThanOrEqual(shortResult.score);
        },
      ),
    );
  });
});

describe('PasswordStrengthMeter', () => {
  it('should render nothing when password is empty', () => {
    const { container } = render(<PasswordStrengthMeter password="" />);
    expect(container.innerHTML).toBe('');
  });

  it('should render strength label when password is not empty', () => {
    render(<PasswordStrengthMeter password="a" />);
    expect(screen.getByText('Password strength')).toBeInTheDocument();
  });

  it('should render all requirement labels', () => {
    render(<PasswordStrengthMeter password="a" />);
    expect(screen.getByText('At least 8 characters')).toBeInTheDocument();
    expect(screen.getByText('One uppercase letter')).toBeInTheDocument();
    expect(screen.getByText('One lowercase letter')).toBeInTheDocument();
    expect(screen.getByText('One number')).toBeInTheDocument();
  });

  it('should render bonus labels', () => {
    render(<PasswordStrengthMeter password="a" />);
    expect(screen.getByText('Special character')).toBeInTheDocument();
    expect(screen.getByText('12+ characters')).toBeInTheDocument();
  });

  it('should show Too weak for single character', () => {
    render(<PasswordStrengthMeter password="a" />);
    expect(screen.getByText('Too weak')).toBeInTheDocument();
  });

  it('should show Very strong for full-strength password', () => {
    render(<PasswordStrengthMeter password="MyP@ssword123" />);
    expect(screen.getByText('Very strong')).toBeInTheDocument();
  });

  it('should have accessible status role', () => {
    render(<PasswordStrengthMeter password="test" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
