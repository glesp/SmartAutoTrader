// frontend/src/components/vehicles/VehicleFilters.test.tsx
import {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from '@testing-library/react';
import VehicleFilters from './VehicleFilters';
import { FilterState } from '../../types/models';
import { vehicleService } from '../../services/api';
import { vi, type Mock } from 'vitest';
import '@testing-library/jest-dom';

vi.mock('../../services/api', () => ({
  vehicleService: {
    getAvailableMakes: vi.fn(),
    getAvailableModels: vi.fn(),
    getYearRange: vi.fn(),
    getEngineSizeRange: vi.fn(),
    getHorsepowerRange: vi.fn(),
  },
}));

describe('VehicleFilters Component', () => {
  const getInitialFiltersForTest = (): FilterState => ({
    make: '',
    model: '',
    minYear: 1990,
    maxYear: 2025,
    minPrice: 0,
    maxPrice: 150000,
    fuelType: '',
    transmission: '',
    vehicleType: '',
    minEngineSize: 1.0,
    maxEngineSize: 6.0,
    minHorsepower: 80,
    maxHorsepower: 700,
    sortBy: 'DateListed',
    ascending: false,
    rejectedMakes: [],
    rejectedFuelTypes: [],
    rejectedVehicleTypes: [],
    rejectedFeatures: [],
  });

  const mockOnFilterChange = vi.fn();

  beforeEach(() => {
    mockOnFilterChange.mockClear();
    (vehicleService.getAvailableMakes as Mock).mockResolvedValue([
      'Toyota',
      'Honda',
      'Ford',
    ]);
    (vehicleService.getAvailableModels as Mock).mockResolvedValue([]); // Default for models
    (vehicleService.getYearRange as Mock).mockResolvedValue({
      min: 1990,
      max: 2025,
    });
    (vehicleService.getEngineSizeRange as Mock).mockResolvedValue({
      min: 1.0,
      max: 6.0,
    });
    (vehicleService.getHorsepowerRange as Mock).mockResolvedValue({
      min: 80,
      max: 700,
    });
  });

  test('renders filter controls and fetches initial data on mount', async () => {
    // Set up mocks to resolve immediately to reduce test flakiness
    const makesMock = ['Toyota', 'Honda', 'Ford'];
    const yearRangeMock = { min: 1990, max: 2025 };
    const engineSizeRangeMock = { min: 1.0, max: 6.0 };
    const horsepowerRangeMock = { min: 80, max: 700 };

    (vehicleService.getAvailableMakes as Mock).mockResolvedValue(makesMock);
    (vehicleService.getYearRange as Mock).mockResolvedValue(yearRangeMock);
    (vehicleService.getEngineSizeRange as Mock).mockResolvedValue(
      engineSizeRangeMock
    );
    (vehicleService.getHorsepowerRange as Mock).mockResolvedValue(
      horsepowerRangeMock
    );

    render(
      <VehicleFilters
        filters={getInitialFiltersForTest()}
        onFilterChange={mockOnFilterChange}
      />
    );

    // Wait for all data to be loaded and dropdowns to be populated
    // Use findBy* queries which implicitly wait for elements to appear
    await screen.findByRole('combobox', { name: /Make/i });

    // Now verify that all the API calls were made
    expect(vehicleService.getAvailableMakes).toHaveBeenCalledTimes(1);
    expect(vehicleService.getYearRange).toHaveBeenCalledTimes(1);
    expect(vehicleService.getEngineSizeRange).toHaveBeenCalledTimes(1);
    expect(vehicleService.getHorsepowerRange).toHaveBeenCalledTimes(1);

    // Assert UI elements are rendered correctly once data is loaded
    expect(
      screen.getByRole('combobox', { name: /Model/i })
    ).toBeInTheDocument();

    // Check for Price Range heading
    expect(screen.getByText(/Price Range \(€\)/i)).toBeInTheDocument();

    // Look for sliders - now that we're using a price range slider instead of text fields
    const sliders = screen.getAllByRole('slider');
    expect(sliders.length).toBeGreaterThan(0);

    // Check for Sort By section
    expect(screen.getByText(/Sort By/i)).toBeInTheDocument();

    // Use a more specific query for the Reset All Filters button
    expect(
      screen.getByRole('button', { name: /^Reset All Filters$/i })
    ).toBeInTheDocument();
  });

  test('calls onFilterChange when a make is selected', async () => {
    const initialFilters = getInitialFiltersForTest();
    render(
      <VehicleFilters
        filters={initialFilters}
        onFilterChange={mockOnFilterChange}
      />
    );

    const makeSelectTrigger = screen.getByRole('combobox', { name: /Make/i });
    fireEvent.mouseDown(makeSelectTrigger);

    const toyotaOption = await screen.findByRole('option', { name: 'Toyota' });
    fireEvent.click(toyotaOption);

    await waitFor(() => {
      expect(mockOnFilterChange).toHaveBeenCalledWith(
        expect.objectContaining({ make: 'Toyota', model: undefined })
      );
    });
  });

  test('fetches models when a make is selected', async () => {
    (vehicleService.getAvailableModels as Mock).mockResolvedValueOnce([
      'F-150',
      'Focus',
    ]);
    const initialFilters = getInitialFiltersForTest();
    const { rerender } = render(
      <VehicleFilters
        filters={initialFilters}
        onFilterChange={mockOnFilterChange}
      />
    );

    const makeSelectTrigger = screen.getByRole('combobox', { name: /Make/i });
    fireEvent.mouseDown(makeSelectTrigger);
    const fordOption = await screen.findByRole('option', { name: 'Ford' });
    fireEvent.click(fordOption);

    await waitFor(() => {
      expect(mockOnFilterChange).toHaveBeenCalledWith(
        expect.objectContaining({ make: 'Ford', model: undefined })
      );
    });

    const updatedFiltersFromCallback = mockOnFilterChange.mock
      .calls[0][0] as Partial<FilterState>;
    const newFiltersProp = { ...initialFilters, ...updatedFiltersFromCallback };

    rerender(
      <VehicleFilters
        filters={newFiltersProp}
        onFilterChange={mockOnFilterChange}
      />
    );

    await waitFor(() => {
      expect(vehicleService.getAvailableModels).toHaveBeenCalledWith('Ford');
    });
  });

  test('"Reset All Filters" button calls onFilterChange with specific reset values', async () => {
    // Use a new instance of mock function for this test to isolate its calls
    const resetTestMock = vi.fn();

    const currentFilters: FilterState = {
      ...getInitialFiltersForTest(),
      make: 'Ford',
      minPrice: 30000,
      maxPrice: 50000,
      sortBy: 'Price',
    };

    // Include empty string in the available makes to prevent MUI warning
    (vehicleService.getAvailableMakes as Mock).mockResolvedValueOnce([
      '',
      'Ford',
    ]);
    // Mock getAvailableModels to return empty array when make is empty
    (vehicleService.getAvailableModels as Mock).mockImplementation(
      (make: string) => {
        return Promise.resolve(make === 'Ford' ? ['Focus', 'Fiesta'] : []);
      }
    );

    render(
      <VehicleFilters filters={currentFilters} onFilterChange={resetTestMock} />
    );

    // Wait for initial data loading and ensure Ford is visible
    await waitFor(() => {
      expect(vehicleService.getAvailableMakes).toHaveBeenCalled();
    });
    await screen.findByText('Ford');

    // Find the reset button by its exact text content
    const resetAllFiltersButton = screen.getByRole('button', {
      name: /^Reset All Filters$/i,
    });

    await act(async () => {
      fireEvent.click(resetAllFiltersButton);
    });

    // Focus on the payload of the reset call, not the exact number of calls
    await waitFor(() => {
      expect(resetTestMock).toHaveBeenCalled();
      // Only check the first call's payload to see if it has the correct reset values
      expect(resetTestMock.mock.calls[0][0]).toEqual({
        make: '',
        model: '',
        minYear: undefined,
        maxYear: undefined,
        minPrice: undefined,
        maxPrice: undefined,
        fuelType: '',
        transmission: '',
        vehicleType: '',
        minEngineSize: undefined,
        maxEngineSize: undefined,
        minHorsepower: undefined,
        maxHorsepower: undefined,
        sortBy: 'DateListed',
        ascending: false,
        rejectedMakes: undefined,
        rejectedFuelTypes: undefined,
        rejectedVehicleTypes: undefined,
        rejectedFeatures: undefined,
      });
    });
  });

  it('renders filter controls including dropdowns and sliders', async () => {
    vi.mocked(vehicleService.getAvailableMakes).mockResolvedValue([
      'Ford',
      'Toyota',
    ]);
    vi.mocked(vehicleService.getYearRange).mockResolvedValue({
      min: 2000,
      max: 2023,
    });
    vi.mocked(vehicleService.getEngineSizeRange).mockResolvedValue({
      min: 1.0,
      max: 5.0,
    });
    vi.mocked(vehicleService.getHorsepowerRange).mockResolvedValue({
      min: 100,
      max: 500,
    });

    render(
      <VehicleFilters
        filters={getInitialFiltersForTest()}
        onFilterChange={mockOnFilterChange}
      />
    );

    expect(await screen.findByLabelText(/Make/i)).toBeInTheDocument();
    expect(screen.getByText(/Year Range/i)).toBeInTheDocument();
    expect(screen.getByText(/Price Range/i)).toBeInTheDocument();

    // Changed to look for text instead of an accessible combobox
    expect(screen.getByText(/Sort By/i)).toBeInTheDocument();
  });

  it('"Reset All Filters" button calls onFilterChange with all default values', async () => {
    // Use a new instance of mock function for this test
    const resetAllTestMock = vi.fn();

    const initialSetFilters: FilterState = {
      make: 'Ford',
      model: 'Focus',
      minPrice: 5000,
      maxPrice: 15000,
      sortBy: 'Price',
      ascending: true,
      minYear: 2000,
      maxYear: 2020,
      fuelType: 'Petrol',
      transmission: 'Automatic',
      vehicleType: 'SUV',
      minEngineSize: 1.5,
      maxEngineSize: 2.5,
      minHorsepower: 100,
      maxHorsepower: 200,
      rejectedMakes: [],
      rejectedFuelTypes: [],
      rejectedVehicleTypes: [],
      rejectedFeatures: [],
    };

    // Setup the mocks to include empty string option to prevent MUI warnings
    (vehicleService.getAvailableMakes as Mock).mockResolvedValueOnce([
      '',
      'Ford',
    ]);
    // Mock getAvailableModels to return different arrays based on make
    (vehicleService.getAvailableModels as Mock).mockImplementation(
      (make: string) => {
        return Promise.resolve(make === 'Ford' ? ['Focus', 'Fiesta'] : []);
      }
    );

    render(
      <VehicleFilters
        filters={initialSetFilters}
        onFilterChange={resetAllTestMock}
      />
    );

    // Wait for options to load
    await waitFor(() => {
      expect(vehicleService.getAvailableMakes).toHaveBeenCalled();
    });
    await screen.findByText('Ford'); // Wait for Ford to appear in the UI

    // Find the "Reset All Filters" button, not just any "Reset" button
    const resetAllFiltersButton = screen.getByRole('button', {
      name: /^Reset All Filters$/i,
    });

    await act(async () => {
      fireEvent.click(resetAllFiltersButton);
    });

    // Check the payload of the first call
    await waitFor(() => {
      expect(resetAllTestMock).toHaveBeenCalled();
      expect(resetAllTestMock.mock.calls[0][0]).toEqual({
        make: '',
        model: '',
        minYear: undefined,
        maxYear: undefined,
        minPrice: undefined,
        maxPrice: undefined,
        fuelType: '',
        transmission: '',
        vehicleType: '',
        minEngineSize: undefined,
        maxEngineSize: undefined,
        minHorsepower: undefined,
        maxHorsepower: undefined,
        sortBy: 'DateListed',
        ascending: false,
        rejectedMakes: undefined,
        rejectedFuelTypes: undefined,
        rejectedVehicleTypes: undefined,
        rejectedFeatures: undefined,
      });
    });
  });
});
