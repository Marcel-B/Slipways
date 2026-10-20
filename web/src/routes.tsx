import type { RouteObject } from 'react-router'
import { Layout } from './components/Layout'
import { NotFoundPage } from './pages/NotFoundPage'
import { SlipwayDetailsPage } from './pages/SlipwayDetailsPage'
import { SlipwaysPage } from './pages/SlipwaysPage'

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { index: true, element: <SlipwaysPage /> },
      { path: 'slipways/:id', element: <SlipwayDetailsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
