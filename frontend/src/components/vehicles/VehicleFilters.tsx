// src/components/vehicles/VehicleFilters.tsx
import { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  TextField,
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
  InputAdornment,
  IconButton,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import { vehicleService } from '../../services/api';
import { FilterState } from '../../types/models';

interface VehicleFiltersProps {
  filters: FilterState;
  onFilterChange: (filters: Partial<FilterState>) => void;
}

// These will be fetched from API but we include fallbacks
const fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid', 'Plugin Hybrid'];
const transmissionTypes = ['Manual', 'Automatic', 'Semi-Automatic'];
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

const VehicleFilters = ({ filters, onFilterChange }: VehicleFiltersProps) => {
  // State for available makes and models from database
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

  // --- Local slider states for debounced filter updates ---
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

  // Fetch available makes and ranges ONCE when component mounts
  useEffect(() => {
    const fetchFilterData = async () => {
      // Renamed function
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
          // REMOVED the setLocalEngineSizeRange call from here
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
          // REMOVED the setLocalHorsepowerRange call from here
        }
      } catch (error) {
        console.error('Error fetching vehicle specifications:', error);
      }
    };

    fetchFilterData();
  }, []); // <-- THE FIX: Changed dependency array to empty []

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

  // Reset all filters
  const handleResetFilters = () => {
    onFilterChange({
      make: undefined,
      model: undefined,
      minYear: undefined,
      maxYear: undefined,
      minPrice: undefined,
      maxPrice: undefined,
      fuelType: undefined,
      transmission: undefined,
      vehicleType: undefined,
      minEngineSize: undefined,
      maxEngineSize: undefined,
      minHorsepower: undefined,
      maxHorsepower: undefined,
      sortBy: 'DateListed',
      ascending: false,
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

      {/* Price range */}
      <Box sx={{ mt: 4, mb: 2 }}>
        <Typography gutterBottom>Price Range (€)</Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <TextField
            label="Min"
            type="number"
            size="small"
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">€</InputAdornment>
              ),
            }}
            value={filters.minPrice || ''}
            onChange={(e) =>
              onFilterChange({
                minPrice: e.target.value ? parseInt(e.target.value) : undefined,
              })
            }
          />
          <TextField
            label="Max"
            type="number"
            size="small"
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">€</InputAdornment>
              ),
            }}
            value={filters.maxPrice || ''}
            onChange={(e) =>
              onFilterChange({
                maxPrice: e.target.value ? parseInt(e.target.value) : undefined,
              })
            }
          />
        </Box>
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
            value={filters.sortBy}
            onChange={(e) => onFilterChange({ sortBy: e.target.value })}
            endAdornment={
              <IconButton
                size="small"
                edge="end"
                onClick={(e) => {
                  e.stopPropagation();
                  toggleSortDirection();
                }}
                sx={{ mr: 2 }}
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
