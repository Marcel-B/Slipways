import type { RouteObject } from 'react-router'
import { Layout } from './components/Layout'
import { DatenschutzPage } from './pages/DatenschutzPage'
import { ImpressumPage } from './pages/ImpressumPage'
import { MarinaDetailsPage } from './pages/MarinaDetailsPage'
import { MarinasPage } from './pages/MarinasPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { SlipwayDetailsPage } from './pages/SlipwayDetailsPage'
import { SlipwaysPage } from './pages/SlipwaysPage'
import { SupportPage } from './pages/SupportPage'
import { WaterDetailsPage } from './pages/WaterDetailsPage'
import { WatersPage } from './pages/WatersPage'

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { index: true, element: <SlipwaysPage /> },
      { path: 'slipways/:id', element: <SlipwayDetailsPage /> },
      { path: 'waters', element: <WatersPage /> },
      { path: 'waters/:id', element: <WaterDetailsPage /> },
      { path: 'marinas', element: <MarinasPage /> },
      { path: 'marinas/:id', element: <MarinaDetailsPage /> },
      { path: 'impressum', element: <ImpressumPage /> },
      { path: 'datenschutz', element: <DatenschutzPage /> },
      { path: 'support', element: <SupportPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
