/**
 * @file VehicleFilters.tsx
 * @summary Defines the `VehicleFilters` component, which provides a user interface for filtering vehicle search results.
 *
 * @description The `VehicleFilters` component allows users to filter vehicles based on various criteria such as make, model, year, price, fuel type, transmission, and more.
 * It includes sliders, dropdowns, and other input elements to dynamically update the filters. The component is styled using Material-UI and is designed to be responsive and user-friendly.
 *
 * @remarks
 * - The component uses Material-UI for layout and styling, including components such as `Box`, `Slider`, `Select`, and `Accordion`.
 * - It fetches available filter options (e.g., makes, models, year ranges) from the backend using the `vehicleService`.
 * - The component synchronizes its local state with the parent component's filter state and provides debounced updates for sliders.
 * - It includes a reset functionality to clear all filters and a sort functionality to order results by various criteria.
 *
 * @dependencies
 * - Material-UI components: `Box`, `Typography`, `Slider`, `Select`, `Accordion`, `Button`, etc.
 * - Material-UI icons: `ExpandMoreIcon`, `ArrowUpwardIcon`, `ArrowDownwardIcon`, `RestartAltIcon`.
 * - Services: `vehicleService` for fetching filter data.
 * - Types: `FilterState` for defining the structure of the filter state.
 */

import { useState, useEffect, JSX } from 'react';
import {
  Box,
  Typography,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Slider,
  Button,
  Divider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  IconButton,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import { vehicleService } from '../../services/api';
import { FilterState } from '../../types/models';

/**
 * @interface VehicleFiltersProps
 * @summary Defines the props for the `VehicleFilters` component.
 *
 * @property {FilterState} filters - The current filter state.
 * @property {(filters: Partial<FilterState>) => void} onFilterChange - Callback function to update the filter state.
 */
interface VehicleFiltersProps {
  filters: FilterState;
  onFilterChange: (filters: Partial<FilterState>) => void;
}

/**
 * @constant fuelTypes
 * @summary A list of available fuel types for filtering.
 */
const fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid', 'Plugin Hybrid'];

/**
 * @constant transmissionTypes
 * @summary A list of available transmission types for filtering.
 */
const transmissionTypes = ['Manual', 'Automatic', 'Semi-Automatic'];

/**
 * @constant vehicleTypes
 * @summary A list of available vehicle types for filtering.
 */
const vehicleTypes = [
  'Sedan',
  'SUV',
  'Hatchback',
  'Coupe',
  'Convertible',
  'Wagon',
  'Van',
  'Truck',
];

/**
 * @function VehicleFilters
 * @summary Renders the vehicle filters UI for filtering search results.
 *
 * @param {VehicleFiltersProps} props - The props for the component.
 * @returns {JSX.Element} The rendered vehicle filters component.
 *
 * @remarks
 * - The component fetches filter options (e.g., makes, models, ranges) from the backend on mount.
 * - It synchronizes local state with the parent filter state and provides debounced updates for sliders.
 * - Includes advanced filters in an expandable accordion and a reset button to clear all filters.
 *
 * @example
 * <VehicleFilters
 *   filters={currentFilters}
 *   onFilterChange={(updatedFilters) => setFilters(updatedFilters)}
 * />
 */
const VehicleFilters = ({
  filters,
  onFilterChange,
}: VehicleFiltersProps): JSX.Element => {
  const [availableMakes, setAvailableMakes] = useState<string[]>([]);
  const [availableModels, setAvailableModels] = useState<string[]>([]);
  const [yearRange, setYearRange] = useState<[number, number]>([
    1990,
    new Date().getFullYear(),
  ]);
  const [engineSizeRange, setEngineSizeRange] = useState<[number, number]>([
    0, 8,
  ]);
  const [horsepowerRange, setHorsepowerRange] = useState<[number, number]>([
    0, 800,
  ]);
  const [priceSliderDefBounds] = useState<[number, number]>([0, 200000]);

  const [localYearRange, setLocalYearRange] = useState<[number, number]>([
    filters.minYear ?? yearRange[0],
    filters.maxYear ?? yearRange[1],
  ]);
  const [localEngineSizeRange, setLocalEngineSizeRange] = useState<
    [number, number]
  >([
    typeof filters.minEngineSize === 'number'
      ? filters.minEngineSize
      : engineSizeRange[0],
    typeof filters.maxEngineSize === 'number'
      ? filters.maxEngineSize
      : engineSizeRange[1],
  ]);
  const [localHorsepowerRange, setLocalHorsepowerRange] = useState<
    [number, number]
  >([
    typeof filters.minHorsepower === 'number'
      ? filters.minHorsepower
      : horsepowerRange[0],
    typeof filters.maxHorsepower === 'number'
      ? filters.maxHorsepower
      : horsepowerRange[1],
  ]);
  const [localPriceRange, setLocalPriceRange] = useState<[number, number]>([
    filters.minPrice ?? priceSliderDefBounds[0],
    filters.maxPrice ?? priceSliderDefBounds[1],
  ]);

  // Sync local slider state with parent filter changes
  useEffect(() => {
    setLocalYearRange([
      filters.minYear ?? yearRange[0],
      filters.maxYear ?? yearRange[1],
    ]);
  }, [filters.minYear, filters.maxYear, yearRange]);

  useEffect(() => {
    setLocalEngineSizeRange([
      typeof filters.minEngineSize === 'number'
        ? filters.minEngineSize
        : engineSizeRange[0],
      typeof filters.maxEngineSize === 'number'
        ? filters.maxEngineSize
        : engineSizeRange[1],
    ]);
  }, [filters.minEngineSize, filters.maxEngineSize, engineSizeRange]);

  useEffect(() => {
    setLocalHorsepowerRange([
      typeof filters.minHorsepower === 'number'
        ? filters.minHorsepower
        : horsepowerRange[0],
      typeof filters.maxHorsepower === 'number'
        ? filters.maxHorsepower
        : horsepowerRange[1],
    ]);
  }, [filters.minHorsepower, filters.maxHorsepower, horsepowerRange]);

  // New useEffect for price range sync
  useEffect(() => {
    setLocalPriceRange([
      filters.minPrice ?? priceSliderDefBounds[0],
      filters.maxPrice ?? priceSliderDefBounds[1],
    ]);
  }, [filters.minPrice, filters.maxPrice, priceSliderDefBounds]);

  // Fetch available makes and ranges ONCE when component mounts
  useEffect(() => {
    const fetchFilterData = async () => {
      try {
        // Fetch makes
        const makesResponse = await vehicleService.getAvailableMakes();
        if (Array.isArray(makesResponse)) {
          setAvailableMakes(makesResponse);
        }

        // Fetch year range
        const yearRangeResponse = await vehicleService.getYearRange();
        if (
          yearRangeResponse &&
          typeof yearRangeResponse.min === 'number' &&
          typeof yearRangeResponse.max === 'number'
        ) {
          setYearRange([yearRangeResponse.min, yearRangeResponse.max]);
        }

        // Fetch engine size range
        const engineSizeRangeResponse =
          await vehicleService.getEngineSizeRange();
        if (
          engineSizeRangeResponse &&
          typeof engineSizeRangeResponse.min === 'number' &&
          typeof engineSizeRangeResponse.max === 'number'
        ) {
          setEngineSizeRange([
            engineSizeRangeResponse.min,
            engineSizeRangeResponse.max,
          ]);
        }

        // Fetch horsepower range
        const horsepowerRangeResponse =
          await vehicleService.getHorsepowerRange();
        if (
          horsepowerRangeResponse &&
          typeof horsepowerRangeResponse.min === 'number' &&
          typeof horsepowerRangeResponse.max === 'number'
        ) {
          setHorsepowerRange([
            horsepowerRangeResponse.min,
            horsepowerRangeResponse.max,
          ]);
        }

        // Note for future: Add vehicleService.getPriceRange() here when available
      } catch (error) {
        console.error('Error fetching vehicle specifications:', error);
      }
    };

    fetchFilterData();
  }, []);

  // Fetch models when make changes
  useEffect(() => {
    const fetchModels = async () => {
      if (!filters.make) {
        setAvailableModels([]);
        return;
      }

      try {
        const modelsResponse = await vehicleService.getAvailableModels(
          filters.make
        );
        if (Array.isArray(modelsResponse)) {
          setAvailableModels(modelsResponse);
        }
      } catch (error) {
        console.error('Error fetching models:', error);
      }
    };

    fetchModels();
  }, [filters.make]);

  // --- Slider handlers for debounced updates ---

  // Year Range
  const handleYearChange = (_event: Event, newValue: number | number[]) => {
    if (Array.isArray(newValue))
      setLocalYearRange(newValue as [number, number]);
  };
  const handleYearChangeCommitted = (
    _event: Event | React.SyntheticEvent,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue)) {
      onFilterChange({
        minYear: newValue[0],
        maxYear: newValue[1],
      });
    }
  };

  // Engine Size Range
  const handleEngineSizeChange = (
    _event: Event,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue))
      setLocalEngineSizeRange(newValue as [number, number]);
  };
  const handleEngineSizeChangeCommitted = (
    _event: Event | React.SyntheticEvent,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue)) {
      onFilterChange({
        minEngineSize: newValue[0],
        maxEngineSize: newValue[1],
      });
    }
  };

  // Horsepower Range
  const handleHorsepowerChange = (
    _event: Event,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue))
      setLocalHorsepowerRange(newValue as [number, number]);
  };
  const handleHorsepowerChangeCommitted = (
    _event: Event | React.SyntheticEvent,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue)) {
      onFilterChange({
        minHorsepower: newValue[0],
        maxHorsepower: newValue[1],
      });
    }
  };

  // 3. Price Range slider handlers
  const handlePriceSliderChange = (
    _event: Event,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue))
      setLocalPriceRange(newValue as [number, number]);
  };

  const handlePriceSliderChangeCommitted = (
    _event: Event | React.SyntheticEvent,
    newValue: number | number[]
  ) => {
    if (Array.isArray(newValue)) {
      onFilterChange({
        minPrice: newValue[0],
        maxPrice: newValue[1],
      });
    }
  };

  // Reset all filters
  const handleResetFilters = () => {
    onFilterChange({
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
  };

  // Toggle sort direction
  const toggleSortDirection = () => {
    onFilterChange({ ascending: !filters.ascending });
  };

  return (
    <Box>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 2,
        }}
      >
        <Typography variant="h6" component="h2" fontWeight="medium">
          Filters
        </Typography>
        <Button
          startIcon={<RestartAltIcon />}
          size="small"
          onClick={handleResetFilters}
          color="inherit"
        >
          Reset
        </Button>
      </Box>

      <Divider sx={{ mb: 3 }} />

      {/* Make dropdown */}
      <FormControl fullWidth margin="normal" size="small">
        <InputLabel id="make-label">Make</InputLabel>
        <Select
          labelId="make-label"
          id="make"
          data-testid="make-select"
          value={filters.make || ''}
          label="Make"
          onChange={(e) =>
            onFilterChange({
              make: e.target.value || undefined,
              model: undefined, // Reset model when make changes
            })
          }
        >
          <MenuItem value="">Any make</MenuItem>
          {availableMakes.map((make) => (
            <MenuItem key={make} value={make}>
              {make}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {/* Model dropdown (only enabled if make is selected) */}
      <FormControl
        fullWidth
        margin="normal"
        size="small"
        disabled={!filters.make}
      >
        <InputLabel id="model-label">Model</InputLabel>
        <Select
          labelId="model-label"
          id="model"
          data-testid="model-select"
          value={filters.model || ''}
          label="Model"
          onChange={(e) =>
            onFilterChange({ model: e.target.value || undefined })
          }
        >
          <MenuItem value="">Any model</MenuItem>
          {availableModels.map((model) => (
            <MenuItem key={model} value={model}>
              {model}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {/* Year range slider */}
      <Box sx={{ mt: 3, mb: 2 }}>
        <Typography gutterBottom>Year Range</Typography>
        <Slider
          value={localYearRange}
          onChange={handleYearChange}
          onChangeCommitted={handleYearChangeCommitted}
          valueLabelDisplay="auto"
          min={yearRange[0]}
          max={yearRange[1]}
          marks={[
            { value: yearRange[0], label: yearRange[0].toString() },
            { value: yearRange[1], label: yearRange[1].toString() },
          ]}
        />
      </Box>

      {/* Price range - Updated to use a slider */}
      <Box sx={{ mt: 4, mb: 2 }}>
        <Typography gutterBottom>Price Range (€)</Typography>
        <Slider
          value={localPriceRange}
          onChange={handlePriceSliderChange}
          onChangeCommitted={handlePriceSliderChangeCommitted}
          valueLabelDisplay="auto"
          min={priceSliderDefBounds[0]}
          max={priceSliderDefBounds[1]}
          step={1000}
          marks={[
            {
              value: priceSliderDefBounds[0],
              label: `€${priceSliderDefBounds[0].toLocaleString()}`,
            },
            {
              value: priceSliderDefBounds[1],
              label: `€${priceSliderDefBounds[1].toLocaleString()}`,
            },
          ]}
          valueLabelFormat={(value) => `€${value.toLocaleString()}`}
        />
      </Box>

      {/* Advanced filters in accordion */}
      <Accordion
        sx={{
          mt: 2,
          mb: 2,
          boxShadow: 'none',
          '&:before': { display: 'none' },
        }}
      >
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography fontWeight="medium">Advanced Filters</Typography>
        </AccordionSummary>
        <AccordionDetails sx={{ px: 1 }}>
          {/* Fuel type */}
          <FormControl fullWidth margin="normal" size="small">
            <InputLabel id="fuel-type-label">Fuel Type</InputLabel>
            <Select
              labelId="fuel-type-label"
              id="fuel-type"
              data-testid="fuel-type-select"
              value={filters.fuelType || ''}
              label="Fuel Type"
              onChange={(e) =>
                onFilterChange({ fuelType: e.target.value || undefined })
              }
            >
              <MenuItem value="">Any fuel type</MenuItem>
              {fuelTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Transmission */}
          <FormControl fullWidth margin="normal" size="small">
            <InputLabel id="transmission-label">Transmission</InputLabel>
            <Select
              labelId="transmission-label"
              id="transmission"
              data-testid="transmission-select"
              value={filters.transmission || ''}
              label="Transmission"
              onChange={(e) =>
                onFilterChange({ transmission: e.target.value || undefined })
              }
            >
              <MenuItem value="">Any transmission</MenuItem>
              {transmissionTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Vehicle type */}
          <FormControl fullWidth margin="normal" size="small">
            <InputLabel id="vehicle-type-label">Vehicle Type</InputLabel>
            <Select
              labelId="vehicle-type-label"
              id="vehicle-type"
              data-testid="vehicle-type-select"
              value={filters.vehicleType || ''}
              label="Vehicle Type"
              onChange={(e) =>
                onFilterChange({ vehicleType: e.target.value || undefined })
              }
            >
              <MenuItem value="">Any vehicle type</MenuItem>
              {vehicleTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Engine Size Range slider */}
          <Box sx={{ mt: 3, mb: 2 }}>
            <Typography gutterBottom>Engine Size (L)</Typography>
            <Slider
              value={localEngineSizeRange}
              onChange={handleEngineSizeChange}
              onChangeCommitted={handleEngineSizeChangeCommitted}
              valueLabelDisplay="auto"
              min={engineSizeRange[0]}
              max={engineSizeRange[1]}
              step={0.1}
              marks={[
                {
                  value: engineSizeRange[0],
                  label: engineSizeRange[0].toFixed(1),
                },
                {
                  value: engineSizeRange[1],
                  label: engineSizeRange[1].toFixed(1),
                },
              ]}
            />
          </Box>

          {/* Horsepower Range slider */}
          <Box sx={{ mt: 3, mb: 2 }}>
            <Typography gutterBottom>Horsepower (HP)</Typography>
            <Slider
              value={localHorsepowerRange}
              onChange={handleHorsepowerChange}
              onChangeCommitted={handleHorsepowerChangeCommitted}
              valueLabelDisplay="auto"
              min={horsepowerRange[0]}
              max={horsepowerRange[1]}
              step={10}
              marks={[
                {
                  value: horsepowerRange[0],
                  label: horsepowerRange[0].toString(),
                },
                {
                  value: horsepowerRange[1],
                  label: horsepowerRange[1].toString(),
                },
              ]}
            />
          </Box>
        </AccordionDetails>
      </Accordion>

      <Divider sx={{ my: 3 }} />

      {/* Sort options */}
      <Box>
        <Typography gutterBottom fontWeight="medium">
          Sort By
        </Typography>
        <FormControl fullWidth size="small">
          <Select
            data-testid="sort-by-select"
            value={filters.sortBy}
            onChange={(e) => onFilterChange({ sortBy: e.target.value })}
            aria-label="Sort By"
            endAdornment={
              <IconButton
                size="small"
                edge="end"
                onClick={(e) => {
                  e.stopPropagation();
                  toggleSortDirection();
                }}
                sx={{ mr: 2 }}
                aria-label={
                  filters.ascending
                    ? 'Sort in descending order'
                    : 'Sort in ascending order'
                }
              >
                {filters.ascending ? (
                  <ArrowUpwardIcon />
                ) : (
                  <ArrowDownwardIcon />
                )}
              </IconButton>
            }
          >
            <MenuItem value="DateListed">Date Listed</MenuItem>
            <MenuItem value="Price">Price</MenuItem>
            <MenuItem value="Year">Year</MenuItem>
            <MenuItem value="Mileage">Mileage</MenuItem>
            <MenuItem value="Make">Make</MenuItem>
            <MenuItem value="Model">Model</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {/* Reset All Filters button at the bottom */}
      <Box sx={{ mt: 4, display: 'flex', justifyContent: 'center' }}>
        <Button
          variant="outlined"
          color="secondary"
          startIcon={<RestartAltIcon />}
          onClick={handleResetFilters}
          size="large"
        >
          Reset All Filters
        </Button>
      </Box>
    </Box>
  );
};

export default VehicleFilters;
