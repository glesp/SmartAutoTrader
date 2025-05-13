// // frontend/src/components/vehicles/VehicleFilters.test.tsx
// import {
//   render,
//   screen,
//   fireEvent,
//   waitFor,
//   act,
// } from '@testing-library/react';
// import VehicleFilters from './VehicleFilters';
// import { FilterState } from '../../types/models';
// import { vehicleService } from '../../services/api';
// import { vi, type Mock } from 'vitest';
// import '@testing-library/jest-dom';

// vi.mock('../../services/api', () => ({
//   vehicleService: {
//     getAvailableMakes: vi.fn(),
//     getAvailableModels: vi.fn(),
//     getYearRange: vi.fn(),
//     getEngineSizeRange: vi.fn(),
//     getHorsepowerRange: vi.fn(),
//   },
// }));

// describe('VehicleFilters Component', () => {
//   const getInitialFiltersForTest = (): FilterState => ({
//     make: '',
//     model: '',
//     minYear: 1990,
//     maxYear: 2025,
//     minPrice: 0,
//     maxPrice: 150000,
//     fuelType: '',
//     transmission: '',
//     vehicleType: '',
//     minEngineSize: 1.0,
//     maxEngineSize: 6.0,
//     minHorsepower: 80,
//     maxHorsepower: 700,
//     sortBy: 'DateListed',
//     ascending: false,
//     rejectedMakes: [],
//     rejectedFuelTypes: [],
//     rejectedVehicleTypes: [],
//     rejectedFeatures: [],
//   });

//   const mockOnFilterChange = vi.fn();

//   beforeEach(() => {
//     mockOnFilterChange.mockClear();
//     (vehicleService.getAvailableMakes as Mock).mockResolvedValue([
//       'Toyota',
//       'Honda',
//       'Ford',
//     ]);
//     (vehicleService.getAvailableModels as Mock).mockResolvedValue([]); // Default for models
//     (vehicleService.getYearRange as Mock).mockResolvedValue({
//       min: 1990,
//       max: 2025,
//     });
//     (vehicleService.getEngineSizeRange as Mock).mockResolvedValue({
//       min: 1.0,
//       max: 6.0,
//     });
//     (vehicleService.getHorsepowerRange as Mock).mockResolvedValue({
//       min: 80,
//       max: 700,
//     });
//   });

//   test('renders filter controls and fetches initial data on mount', async () => {
//     render(
//       <VehicleFilters
//         filters={getInitialFiltersForTest()}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     expect(screen.getByRole('combobox', { name: /Make/i })).toBeInTheDocument();
//     expect(
//       screen.getByRole('combobox', { name: /Model/i })
//     ).toBeInTheDocument();
//     expect(screen.getByLabelText(/^Min$/i)).toBeInTheDocument();
//     expect(screen.getByLabelText(/^Max$/i)).toBeInTheDocument();

//     const sortBySelectWrapper = await screen.findByTestId('sort-by-select');
//     expect(sortBySelectWrapper).toBeInTheDocument();
//     const sortByCombobox =
//       sortBySelectWrapper.querySelector('[role="combobox"]');
//     expect(sortByCombobox).toBeInTheDocument();
//     expect(sortByCombobox).toHaveAccessibleName(/Sort By/i);

//     expect(
//       screen.getByRole('button', { name: /Reset All Filters/i })
//     ).toBeInTheDocument();

//     await waitFor(() => {
//       expect(vehicleService.getAvailableMakes).toHaveBeenCalledTimes(1);
//       expect(vehicleService.getYearRange).toHaveBeenCalledTimes(1);
//       expect(vehicleService.getEngineSizeRange).toHaveBeenCalledTimes(1);
//       expect(vehicleService.getHorsepowerRange).toHaveBeenCalledTimes(1);
//     });
//   });

//   test('calls onFilterChange when a make is selected', async () => {
//     const initialFilters = getInitialFiltersForTest();
//     render(
//       <VehicleFilters
//         filters={initialFilters}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     const makeSelectTrigger = screen.getByRole('combobox', { name: /Make/i });
//     fireEvent.mouseDown(makeSelectTrigger);

//     const toyotaOption = await screen.findByRole('option', { name: 'Toyota' });
//     fireEvent.click(toyotaOption);

//     await waitFor(() => {
//       expect(mockOnFilterChange).toHaveBeenCalledWith(
//         expect.objectContaining({ make: 'Toyota', model: undefined })
//       );
//     });
//   });

//   test('fetches models when a make is selected', async () => {
//     (vehicleService.getAvailableModels as Mock).mockResolvedValueOnce([
//       'F-150',
//       'Focus',
//     ]);
//     const initialFilters = getInitialFiltersForTest();
//     const { rerender } = render(
//       <VehicleFilters
//         filters={initialFilters}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     const makeSelectTrigger = screen.getByRole('combobox', { name: /Make/i });
//     fireEvent.mouseDown(makeSelectTrigger);
//     const fordOption = await screen.findByRole('option', { name: 'Ford' });
//     fireEvent.click(fordOption);

//     await waitFor(() => {
//       expect(mockOnFilterChange).toHaveBeenCalledWith(
//         expect.objectContaining({ make: 'Ford', model: undefined })
//       );
//     });

//     const updatedFiltersFromCallback = mockOnFilterChange.mock
//       .calls[0][0] as Partial<FilterState>;
//     const newFiltersProp = { ...initialFilters, ...updatedFiltersFromCallback };

//     rerender(
//       <VehicleFilters
//         filters={newFiltersProp}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     await waitFor(() => {
//       expect(vehicleService.getAvailableModels).toHaveBeenCalledWith('Ford');
//     });
//   });

//   test('calls onFilterChange when Min Price field is changed and blurred', async () => {
//     const initialFilters = getInitialFiltersForTest();
//     render(
//       <VehicleFilters
//         filters={initialFilters}
//         onFilterChange={mockOnFilterChange}
//       />
//     );
//     const minPriceInput = screen.getByLabelText(/^Min$/i) as HTMLInputElement;

//     fireEvent.change(minPriceInput, { target: { value: '10000' } });
//     fireEvent.blur(minPriceInput);

//     await waitFor(() => {
//       expect(mockOnFilterChange).toHaveBeenCalledWith(
//         expect.objectContaining({ minPrice: 10000 })
//       );
//     });
//   });

//   test('"Reset All Filters" button calls onFilterChange with specific reset values', async () => {
//     const currentFilters: FilterState = {
//       ...getInitialFiltersForTest(),
//       make: 'Ford',
//       minPrice: 30000,
//       maxPrice: 50000,
//       sortBy: 'Price',
//     };

//     render(
//       <VehicleFilters
//         filters={currentFilters}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     await waitFor(() => {
//       expect(vehicleService.getAvailableMakes).toHaveBeenCalledTimes(1);
//     });

//     const resetButton = screen.getByRole('button', {
//       name: /Reset All Filters/i,
//     });
//     await act(async () => {
//       fireEvent.click(resetButton);
//     });

//     await waitFor(() => {
//       expect(mockOnFilterChange).toHaveBeenCalledWith({
//         make: '',
//         model: '',
//         minYear: undefined,
//         maxYear: undefined,
//         minPrice: undefined,
//         maxPrice: undefined,
//         fuelType: '',
//         transmission: '',
//         vehicleType: '',
//         minEngineSize: undefined,
//         maxEngineSize: undefined,
//         minHorsepower: undefined,
//         maxHorsepower: undefined,
//         sortBy: 'DateListed',
//         ascending: false,
//         rejectedMakes: undefined,
//         rejectedFuelTypes: undefined,
//         rejectedVehicleTypes: undefined,
//         rejectedFeatures: undefined,
//       });
//     });
//   });

//   it('renders filter controls including dropdowns and sliders', async () => {
//     vi.mocked(vehicleService.getAvailableMakes).mockResolvedValue([
//       'Ford',
//       'Toyota',
//     ]);
//     vi.mocked(vehicleService.getYearRange).mockResolvedValue({
//       min: 2000,
//       max: 2023,
//     });
//     vi.mocked(vehicleService.getEngineSizeRange).mockResolvedValue({
//       min: 1.0,
//       max: 5.0,
//     });
//     vi.mocked(vehicleService.getHorsepowerRange).mockResolvedValue({
//       min: 100,
//       max: 500,
//     });

//     render(
//       <VehicleFilters
//         filters={getInitialFiltersForTest()}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     expect(await screen.findByLabelText(/Make/i)).toBeInTheDocument();
//     expect(screen.getByText(/Year Range/i)).toBeInTheDocument();
//     expect(screen.getByText(/Price Range/i)).toBeInTheDocument();

//     const sortBySelectWrapper = await screen.findByTestId('sort-by-select');
//     expect(
//       sortBySelectWrapper.querySelector('[role="combobox"]')
//     ).toHaveAccessibleName(/Sort By/i);
//   });

//   it('"Reset All Filters" button calls onFilterChange with all default values', async () => {
//     const initialSetFilters: FilterState = {
//       make: 'Ford',
//       model: 'Focus',
//       minPrice: 5000,
//       maxPrice: 15000,
//       sortBy: 'Price',
//       ascending: true,
//       minYear: 2000,
//       maxYear: 2020,
//       fuelType: 'Petrol',
//       transmission: 'Automatic',
//       vehicleType: 'SUV',
//       minEngineSize: 1.5,
//       maxEngineSize: 2.5,
//       minHorsepower: 100,
//       maxHorsepower: 200,
//       rejectedMakes: [],
//       rejectedFuelTypes: [],
//       rejectedVehicleTypes: [],
//       rejectedFeatures: [],
//     };

//     (vehicleService.getAvailableModels as Mock).mockResolvedValueOnce([
//       'Focus',
//       'Fiesta',
//     ]);

//     render(
//       <VehicleFilters
//         filters={initialSetFilters}
//         onFilterChange={mockOnFilterChange}
//       />
//     );

//     await waitFor(() => {
//       expect(vehicleService.getAvailableMakes).toHaveBeenCalled();
//       if (initialSetFilters.make) {
//         expect(vehicleService.getAvailableModels).toHaveBeenCalledWith(
//           initialSetFilters.make
//         );
//       }
//     });

//     const resetButton = screen.getByRole('button', {
//       name: /Reset All Filters/i,
//     });
//     await act(async () => {
//       fireEvent.click(resetButton);
//     });

//     expect(mockOnFilterChange).toHaveBeenCalledWith({
//       make: '',
//       model: '',
//       minYear: undefined,
//       maxYear: undefined,
//       minPrice: undefined,
//       maxPrice: undefined,
//       fuelType: '',
//       transmission: '',
//       vehicleType: '',
//       minEngineSize: undefined,
//       maxEngineSize: undefined,
//       minHorsepower: undefined,
//       maxHorsepower: undefined,
//       sortBy: 'DateListed',
//       ascending: false,
//       rejectedMakes: undefined,
//       rejectedFuelTypes: undefined,
//       rejectedVehicleTypes: undefined,
//       rejectedFeatures: undefined,
//     });
//   });
// });
