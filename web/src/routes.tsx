import type { RouteObject } from 'react-router'
import { Layout } from './components/Layout'
import { DatenschutzPage } from './pages/DatenschutzPage'
import { ImpressumPage } from './pages/ImpressumPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { SlipwayDetailsPage } from './pages/SlipwayDetailsPage'
import { SlipwaysPage } from './pages/SlipwaysPage'
import { SupportPage } from './pages/SupportPage'

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { index: true, element: <SlipwaysPage /> },
      { path: 'slipways/:id', element: <SlipwayDetailsPage /> },
      { path: 'impressum', element: <ImpressumPage /> },
      { path: 'datenschutz', element: <DatenschutzPage /> },
      { path: 'support', element: <SupportPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
